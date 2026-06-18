namespace Lodestone.Domain;

/// <summary>
/// An installed launcher profile: a base game version paired with the loader installed against it
/// (<see cref="Loader.None"/> for plain vanilla), plus the <c>versions/</c> folder id that backs it.
/// This is the unit a Lodestone "profile" switches between - a specific game version + loader, with its
/// own set of mods.
/// </summary>
public sealed record LoaderProfile(GameVersion GameVersion, Loader Loader, string VersionId)
{
    /// <summary>True for a plain vanilla version (no mod loader).</summary>
    public bool IsVanilla => Loader == Loader.None;

    /// <summary>A short label such as "1.20.1 · Fabric" (just the version for vanilla).</summary>
    public string Label => IsVanilla ? GameVersion.Value : $"{GameVersion.Value} · {Loader.ToDisplayName()}";

    /// <summary>Stable, parseable identity for persistence/selection, e.g. "1.20.1|Fabric".</summary>
    public string Key => $"{GameVersion.Value}|{Loader.ToSlug()}";

    /// <summary>
    /// The loader's own build version, parsed from <see cref="VersionId"/> by each loader's launcher
    /// folder convention - e.g. "0.16.5" from "fabric-loader-0.16.5-1.21.4", "47.2.0" from
    /// "1.20.1-forge-47.2.0", "21.1.65" from "neoforge-21.1.65". <c>null</c> for vanilla, or when the id
    /// doesn't follow the expected shape (callers then fall back to the loader name alone).
    /// </summary>
    public string? LoaderVersion => Loader switch
    {
        // Fabric/Quilt: "<slug>-loader-<loaderVersion>-<mc>".
        Loader.Fabric or Loader.Quilt => Between(VersionId, $"{Loader.ToSlug()}-loader-", $"-{GameVersion.Value}"),
        // Forge: "<mc>-forge-<forgeVersion>".
        Loader.Forge => After(VersionId, "-forge-"),
        // NeoForge: "neoforge-<neoForgeVersion>" (the MC version isn't in the id; it comes from the manifest).
        Loader.NeoForge => After(VersionId, "neoforge-"),
        _ => null,
    };

    /// <summary>
    /// A precise, human-friendly description of the installed build, such as "Fabric loader 0.16.5 · MC
    /// 1.21.4" or "Forge 47.2.0 · MC 1.20.1". Fabric and Quilt include the word "loader" (their loader is
    /// a product distinct from the API); Forge and NeoForge don't. Degrades to the loader name alone when
    /// <see cref="LoaderVersion"/> can't be isolated, and to the bare version for vanilla.
    /// </summary>
    public string PreciseLabel
    {
        get
        {
            if (IsVanilla)
            {
                return GameVersion.Value;
            }

            string name = Loader.ToDisplayName();
            string head = LoaderVersion is not { } version
                ? name
                : Loader is Loader.Fabric or Loader.Quilt ? $"{name} loader {version}" : $"{name} {version}";
            return $"{head} · MC {GameVersion.Value}";
        }
    }

    // The text between prefix and suffix when the value is wrapped by both (and there is something
    // between them), else null - so an id that doesn't match the expected convention degrades gracefully
    // instead of yielding a garbled version.
    private static string? Between(string value, string prefix, string suffix)
        => value.Length > prefix.Length + suffix.Length &&
           value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
           value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..^suffix.Length]
            : null;

    // The text following the last occurrence of marker, else null.
    private static string? After(string value, string marker)
    {
        int index = value.LastIndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index >= 0 && index + marker.Length < value.Length
            ? value[(index + marker.Length)..]
            : null;
    }
}

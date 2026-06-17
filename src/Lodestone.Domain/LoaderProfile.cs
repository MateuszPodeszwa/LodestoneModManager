namespace Lodestone.Domain;

/// <summary>
/// An installed launcher profile: a base game version paired with the loader installed against it
/// (<see cref="Loader.None"/> for plain vanilla), plus the <c>versions/</c> folder id that backs it.
/// This is the unit a Lodestone "profile" switches between — a specific game version + loader, with its
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
}

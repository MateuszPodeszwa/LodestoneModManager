namespace Lodestone.Domain;

/// <summary>
/// A mod loader. <see cref="None"/> is the Null-Object used by content that needs no loader
/// (resource packs and shaders), which keeps loader-agnostic code free of null checks.
/// </summary>
public enum Loader
{
    None = 0,
    Fabric,
    Forge,
    Quilt,
    NeoForge,
}

public static class LoaderExtensions
{
    /// <summary>The canonical lowercase slug used by mod sources and folder conventions.</summary>
    public static string ToSlug(this Loader loader) => loader switch
    {
        Loader.Fabric => "fabric",
        Loader.Forge => "forge",
        Loader.Quilt => "quilt",
        Loader.NeoForge => "neoforge",
        _ => string.Empty,
    };

    /// <summary>Human-friendly label for display.</summary>
    public static string ToDisplayName(this Loader loader) => loader switch
    {
        Loader.Fabric => "Fabric",
        Loader.Forge => "Forge",
        Loader.Quilt => "Quilt",
        Loader.NeoForge => "NeoForge",
        _ => "None",
    };

    /// <summary>Parses a loader slug/name, tolerantly; unknown or empty input yields <see cref="Loader.None"/>.</summary>
    public static Loader ParseLoader(this string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "fabric" => Loader.Fabric,
        "forge" => Loader.Forge,
        "quilt" => Loader.Quilt,
        "neoforge" => Loader.NeoForge,
        _ => Loader.None,
    };
}

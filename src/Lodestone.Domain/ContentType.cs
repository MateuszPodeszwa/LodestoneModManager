namespace Lodestone.Domain;

/// <summary>The kind of content Lodestone manages. Determines the target folder and whether a loader applies.</summary>
public enum ContentType
{
    Mod = 0,
    ResourcePack,
    Shader,
}

/// <summary>Behaviour for <see cref="ContentType"/>: loader applicability, target folder and display name.</summary>
public static class ContentTypeExtensions
{
    /// <summary>Only mods are bound to a loader; packs and shaders are loader-agnostic.</summary>
    public static bool UsesLoader(this ContentType type) => type == ContentType.Mod;

    /// <summary>The <c>.minecraft</c> subfolder this content installs into.</summary>
    public static string ToFolderName(this ContentType type) => type switch
    {
        ContentType.Mod => "mods",
        ContentType.ResourcePack => "resourcepacks",
        ContentType.Shader => "shaderpacks",
        _ => "mods",
    };

    /// <summary>Human-friendly label matching the design's wording.</summary>
    public static string ToDisplayName(this ContentType type) => type switch
    {
        ContentType.Mod => "Mod",
        ContentType.ResourcePack => "Resource Pack",
        ContentType.Shader => "Shader",
        _ => "Mod",
    };
}

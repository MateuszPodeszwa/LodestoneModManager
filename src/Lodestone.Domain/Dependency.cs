namespace Lodestone.Domain;

/// <summary>How one piece of content relates to another it declares.</summary>
public enum DependencyKind
{
    /// <summary>Must be present for this content to work.</summary>
    Required,

    /// <summary>Enhances this content but is not mandatory.</summary>
    Optional,

    /// <summary>Must NOT be present alongside this content.</summary>
    Incompatible,

    /// <summary>Bundled inside this content already (informational; never "missing").</summary>
    Embedded,
}

/// <summary>
/// A declared relationship to another project/mod. <see cref="Identifier"/> is source-specific:
/// a Modrinth <c>project_id</c> for catalog data, or a loader mod-id/slug for locally inspected jars.
/// The compatibility engine resolves identifiers against installed content. <see cref="VersionRange"/>
/// is the version constraint the declaring item asks for (e.g. <c>&gt;=0.100.0</c>), when the source
/// provides one — Fabric/Quilt jar metadata carries it; Modrinth does not, so it stays null there.
/// </summary>
public sealed record Dependency(
    string Identifier,
    DependencyKind Kind,
    string? VersionId = null,
    string? DisplayName = null,
    string? VersionRange = null)
{
    /// <summary>A best-effort label for the UI, falling back to the raw identifier.</summary>
    public string Label => string.IsNullOrWhiteSpace(DisplayName) ? Identifier : DisplayName!;
}

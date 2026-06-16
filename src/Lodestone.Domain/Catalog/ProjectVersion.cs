namespace Lodestone.Domain;

/// <summary>
/// A concrete, downloadable build of a <see cref="CatalogProject"/>: a specific file with its
/// declared game versions, loaders, dependencies and integrity hash. The version resolver picks the
/// best <see cref="ProjectVersion"/> for the active game version + loader before downloading.
/// </summary>
public sealed record ProjectVersion(
    string Id,
    string ProjectId,
    string VersionNumber,
    ContentType Type,
    IReadOnlyList<GameVersion> GameVersions,
    IReadOnlyList<Loader> Loaders,
    IReadOnlyList<Dependency> Dependencies,
    string FileName,
    string DownloadUrl,
    string? Sha512,
    double SizeMb,
    DateTimeOffset? Published = null)
{
    public bool SupportsGameVersion(GameVersion version) => GameVersions.Any(v => v.Equals(version));

    /// <summary>Loader-agnostic content (empty loader list) supports any loader, including <see cref="Loader.None"/>.</summary>
    public bool SupportsLoader(Loader loader)
        => Loaders.Count == 0 || loader == Loader.None || Loaders.Contains(loader);
}

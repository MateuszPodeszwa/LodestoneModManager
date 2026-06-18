namespace Lodestone.Domain;

/// <summary>
/// A project as returned by a mod source's search/browse (Modrinth, CurseForge). This is the
/// "card" data shown on the Browse screen and in the detail modal - not yet a concrete downloadable
/// file (see <see cref="ProjectVersion"/>).
/// </summary>
public sealed record CatalogProject(
    string Id,
    string Slug,
    string Name,
    string Author,
    ContentType Type,
    string Description,
    long Downloads,
    long Followers,
    IReadOnlyList<string> Categories,
    IReadOnlyList<Loader> Loaders,
    IReadOnlyList<GameVersion> GameVersions,
    string Source,
    string? IconUrl = null,
    string? LatestVersion = null,
    string? Body = null,
    IReadOnlyList<string>? GalleryUrls = null)
{
    public bool SupportsLoader(Loader loader)
        => Loaders.Count == 0 || loader == Loader.None || Loaders.Contains(loader);

    public bool SupportsVersion(GameVersion version) => GameVersions.Any(v => v.Equals(version));
}

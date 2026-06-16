using Lodestone.Domain;

namespace Lodestone.Application.Catalog;

/// <summary>Chooses the best downloadable build of a project for a given game version + loader.</summary>
public interface IVersionResolver
{
    ProjectVersion? Resolve(IReadOnlyList<ProjectVersion> versions, GameVersion gameVersion, Loader loader);
}

/// <summary>
/// Picks the newest compatible <see cref="ProjectVersion"/>: filters to those that support the game
/// version and loader, then prefers the most recently published (falling back to version order).
/// This is the guard that stops an incompatible "latest" build from being installed.
/// </summary>
public sealed class VersionResolver : IVersionResolver
{
    public ProjectVersion? Resolve(IReadOnlyList<ProjectVersion> versions, GameVersion gameVersion, Loader loader)
    {
        return versions
            .Where(v => v.SupportsGameVersion(gameVersion) && v.SupportsLoader(loader))
            .OrderByDescending(v => v.Published ?? DateTimeOffset.MinValue)
            .ThenByDescending(v => v.VersionNumber, VersionComparer.Instance)
            .FirstOrDefault();
    }
}

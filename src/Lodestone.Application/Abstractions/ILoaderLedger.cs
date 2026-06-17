using Lodestone.Domain;

namespace Lodestone.Application.Abstractions;

/// <summary>
/// One loader profile Lodestone installed, identified by the <c>versions/</c> directory id the launcher
/// uses. The extra fields make the ledger a self-describing record of what was set up and when.
/// </summary>
public sealed record LoaderInstall(
    string VersionId,
    Loader Loader,
    string GameVersion,
    string LoaderVersion,
    DateTimeOffset InstalledAt);

/// <summary>
/// A persistent ledger of the loader profiles Lodestone has installed — Fabric/Quilt written directly, and
/// Forge/NeoForge created by their official installers. It is the single source of truth for which
/// <c>versions/</c> profiles are Lodestone-managed, so "Reset to clean" removes exactly those and never a
/// loader the user set up outside Lodestone.
/// </summary>
public interface ILoaderLedger
{
    /// <summary>Every loader profile Lodestone currently believes it installed.</summary>
    Task<IReadOnlyList<LoaderInstall>> AllAsync(CancellationToken ct = default);

    /// <summary>Records a loader profile Lodestone installed, replacing any prior entry with the same id.</summary>
    Task RecordAsync(LoaderInstall install, CancellationToken ct = default);

    /// <summary>Drops the given version-ids from the ledger (after they've been removed from disk, or when a
    /// recorded install turned out never to have completed).</summary>
    Task ForgetAsync(IReadOnlyCollection<string> versionIds, CancellationToken ct = default);
}

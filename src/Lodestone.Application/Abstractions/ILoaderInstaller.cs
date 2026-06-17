using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>The outcome of a loader update: whether anything changed, and the versions involved.</summary>
public sealed record LoaderUpdate(bool Changed, string? PreviousVersion, string Version);

/// <summary>
/// Installs a mod loader into the Minecraft directory so it shows up in the vanilla launcher. Fabric
/// and Quilt can be installed directly (their meta APIs return a launcher profile); Forge/NeoForge
/// require their own Java installers and report as unsupported here.
/// </summary>
public interface ILoaderInstaller
{
    bool Supports(Loader loader);

    /// <summary>Best-effort check that the loader already has a profile for this game version.</summary>
    bool IsInstalled(Loader loader, GameVersion gameVersion);

    /// <summary>The newest installed loader build for this game version, or null when none is present.</summary>
    string? InstalledVersion(Loader loader, GameVersion gameVersion);

    /// <summary>Installs the loader for the game version if not already present (idempotent).</summary>
    Task<Result> EnsureInstalledAsync(Loader loader, GameVersion gameVersion, CancellationToken ct = default);

    /// <summary>Installs the latest stable build, upgrading in place when a newer one is available.</summary>
    Task<Result<LoaderUpdate>> UpdateAsync(Loader loader, GameVersion gameVersion, CancellationToken ct = default);

    /// <summary>
    /// Removes every loader profile Lodestone recorded installing (Fabric/Quilt and Forge/NeoForge) from
    /// <c>versions/</c> and their <c>launcher_profiles.json</c> entries, returning how many were removed.
    /// Used by "Reset to clean"; the user's vanilla versions and any loader they installed outside
    /// Lodestone aren't in the ledger, so they're left untouched.
    /// </summary>
    Task<Result<int>> RemoveManagedAsync(CancellationToken ct = default);
}

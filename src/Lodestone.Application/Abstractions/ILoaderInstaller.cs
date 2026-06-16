using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

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

    /// <summary>Installs the loader for the game version if not already present (idempotent).</summary>
    Task<Result> EnsureInstalledAsync(Loader loader, GameVersion gameVersion, CancellationToken ct = default);
}

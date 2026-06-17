using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>
/// Installs the loaders Lodestone can't write directly — Forge and NeoForge — by downloading their
/// official installer and launching it (their own Java GUI does the work). Once the user completes it,
/// the resulting profile is picked up by <see cref="IGameInventory"/>. This is distinct from
/// <see cref="ILoaderInstaller"/>, which installs Fabric/Quilt directly through their meta APIs.
/// </summary>
public interface IExternalLoaderInstaller
{
    bool Supports(Loader loader);

    /// <summary>
    /// Resolves the latest installer for <paramref name="loader"/> + <paramref name="version"/>, downloads
    /// it, and launches it. Returns the launched installer version on success, or a failure: no published
    /// build for that version (<c>loader.no_version</c>), no Java runtime found (<c>loader.no_java</c>),
    /// or a network/IO error. The profile the installer will create is recorded in the loader ledger so a
    /// later reset can remove it.
    /// </summary>
    Task<Result<string>> LaunchInstallerAsync(Loader loader, GameVersion version, CancellationToken ct = default);
}

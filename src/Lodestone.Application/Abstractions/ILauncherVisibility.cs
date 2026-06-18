using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>
/// Controls which modded profiles the vanilla Minecraft launcher shows. Activating a profile hides the
/// other modded profiles - their <c>launcher_profiles.json</c> entries are stashed verbatim (so custom
/// settings like JVM args survive) and restored when that profile is reactivated. Vanilla versions and
/// any non-loader profiles the user created are never touched; nothing is deleted.
/// </summary>
public interface ILauncherVisibility
{
    /// <summary>
    /// Make the launcher show only <paramref name="active"/> (plus vanilla): stash every other modded
    /// profile in <paramref name="allModded"/>, and surface the active one (restoring it from the stash,
    /// or creating an entry if it's new). Best-effort - a missing launcher file is a no-op success.
    /// </summary>
    Result Apply(LoaderProfile? active, IReadOnlyList<LoaderProfile> allModded);

    /// <summary>Restore every stashed profile back into the launcher and clear the stash (used by Reset).</summary>
    Result RestoreAll();
}

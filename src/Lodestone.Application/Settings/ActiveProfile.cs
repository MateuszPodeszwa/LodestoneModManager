using Lodestone.Application.Abstractions;
using Lodestone.Domain;

namespace Lodestone.Application.Settings;

/// <summary>
/// The single source of truth for resolving the "active version" from settings. Replaces the
/// hardcoded <c>1.21.4</c> fallbacks that used to be scattered across the view-models: a concrete
/// install target now comes from what the user selected, or — failing that — the newest version they
/// actually have installed, never a fabricated default.
/// </summary>
public static class ActiveProfile
{
    /// <summary>True for the "All versions" view (or an unset selection).</summary>
    public static bool IsAllVersions(string? selected) => selected is "all" or "" or null;

    /// <summary>The version selected in the UI, or null on the "All versions" view / an unparseable value.</summary>
    public static GameVersion? Selected(LodestoneSettings settings)
        => IsAllVersions(settings.SelectedVersion)
            ? null
            : GameVersion.Create(settings.SelectedVersion).Match<GameVersion?>(v => v, _ => null);

    /// <summary>
    /// A concrete version to install against: the explicit selection, else the newest installed
    /// version, else null when nothing is installed (callers should gate the action in that case).
    /// </summary>
    public static GameVersion? Target(LodestoneSettings settings, IGameInventory inventory)
        => Selected(settings) ?? inventory.InstalledVersions().FirstOrDefault();
}

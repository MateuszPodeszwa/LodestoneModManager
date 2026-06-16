using Lodestone.Domain;

namespace Lodestone.Application.Settings;

public enum UpdateChannel
{
    Stable = 0,
    Beta = 1,
}

/// <summary>
/// User-facing configuration. Every field maps to a control on the Settings (or Onboarding) screen.
/// <see cref="Normalize"/> enforces invariants after load so a hand-edited/corrupt file can't put the
/// app into an invalid state.
/// </summary>
public sealed class LodestoneSettings
{
    public const int MinConcurrentDownloads = 1;
    public const int MaxConcurrentDownloads = 6;
    public const string DefaultSelectedVersion = "1.21.4";

    /// <summary>Bumped when the on-disk shape changes, to drive migrations.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Path to <c>.minecraft</c>. Null until detected/chosen.</summary>
    public string? GameDirectory { get; set; }

    /// <summary>Loader used when installing a mod that supports more than one.</summary>
    public Loader DefaultLoader { get; set; } = Loader.Fabric;

    /// <summary>Keep enabled mods on the latest compatible version (runs on refresh/start).</summary>
    public bool AutoUpdate { get; set; } = true;

    /// <summary>Show a badge on Home when updates are available.</summary>
    public bool NotifyUpdates { get; set; } = true;

    /// <summary>How many files to download at once (clamped to 1..6).</summary>
    public int ConcurrentDownloads { get; set; } = 3;

    /// <summary>Search Modrinth first, fall back to CurseForge when needed.</summary>
    public bool CurseForgeFallback { get; set; } = true;

    /// <summary>
    /// Keep Lodestone resident in the tray when closed. Defaults to <c>false</c> so the process
    /// ends on close (see docs/RISK-ANALYSIS.md §7); even when true there is no background polling.
    /// </summary>
    public bool CloseToTray { get; set; }

    /// <summary>Stable vs Beta auto-update channel (Beta is a supporter perk).</summary>
    public UpdateChannel UpdateChannel { get; set; } = UpdateChannel.Stable;

    public bool OnboardingCompleted { get; set; }

    /// <summary>The game-version profile currently selected in the UI.</summary>
    public string SelectedVersion { get; set; } = DefaultSelectedVersion;

    /// <summary>Optional accent colour override (a supporter cosmetic perk); null = default green.</summary>
    public string? AccentColor { get; set; }

    /// <summary>Clamps/repairs values after loading from disk.</summary>
    public void Normalize()
    {
        ConcurrentDownloads = Math.Clamp(ConcurrentDownloads, MinConcurrentDownloads, MaxConcurrentDownloads);
        if (string.IsNullOrWhiteSpace(SelectedVersion))
        {
            SelectedVersion = DefaultSelectedVersion;
        }
    }

    public LodestoneSettings Clone() => (LodestoneSettings)MemberwiseClone();
}

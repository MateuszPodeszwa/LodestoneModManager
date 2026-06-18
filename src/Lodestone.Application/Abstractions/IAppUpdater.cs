using Lodestone.Application.Settings;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>The outcome of an app update check: whether a newer build exists, the current and latest
/// versions, and whether the offered build is an early-access (semver pre-release) build - so the UI
/// can clearly label it as such.</summary>
public sealed record UpdateCheckResult(bool UpdateAvailable, string CurrentVersion, string? LatestVersion, bool IsPrerelease = false);

/// <summary>
/// App self-update (Velopack adapter). Updates are checked on app start and on demand - there is no
/// background poller. The check, download and apply steps are separated so the UI can download a found
/// update quietly and then let the user choose to restart now or have it applied on next close.
/// </summary>
public interface IAppUpdater
{
    string CurrentVersion { get; }

    /// <summary>Checks the given channel for a newer build and remembers it for a subsequent download/apply.</summary>
    Task<Result<UpdateCheckResult>> CheckAsync(UpdateChannel channel, CancellationToken ct = default);

    /// <summary>Downloads the update found by the last <see cref="CheckAsync"/> (no-op if none) without applying it.</summary>
    Task<Result> DownloadAsync(IProgress<int>? progress = null, CancellationToken ct = default);

    /// <summary>Applies the downloaded update and relaunches into it immediately (the process exits on success).</summary>
    Result ApplyAndRestart();

    /// <summary>Stages the downloaded update so it installs the next time the app closes - no relaunch now.</summary>
    Result ApplyOnExit();
}

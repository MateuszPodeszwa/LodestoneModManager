using Lodestone.Application.Settings;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>The outcome of an app update check: whether a newer build exists, plus the current and latest versions.</summary>
public sealed record UpdateCheckResult(bool UpdateAvailable, string CurrentVersion, string? LatestVersion);

/// <summary>
/// App self-update (Velopack adapter). Updates are checked on demand only — there is no background
/// poller — and applied on next launch.
/// </summary>
public interface IAppUpdater
{
    string CurrentVersion { get; }

    Task<Result<UpdateCheckResult>> CheckAsync(UpdateChannel channel, CancellationToken ct = default);

    Task<Result> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken ct = default);
}

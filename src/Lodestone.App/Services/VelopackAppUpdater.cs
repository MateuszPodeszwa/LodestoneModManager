using System.Reflection;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain.Common;
using Velopack;
using Velopack.Sources;

namespace Lodestone.App.Services;

/// <summary>
/// <see cref="IAppUpdater"/> backed by Velopack against this repo's GitHub Releases. Updates are only
/// checked on demand (no background poller) and applied on next launch. When running from a dev build
/// (not Velopack-installed) it reports "no update" gracefully instead of throwing.
/// </summary>
public sealed class VelopackAppUpdater : IAppUpdater
{
    private const string RepositoryUrl = "https://github.com/MateuszPodeszwa/LodestoneModManager";
    private UpdateInfo? _pending;

    public string CurrentVersion
    {
        get
        {
            try
            {
                UpdateManager manager = CreateManager(UpdateChannel.Stable);
                if (manager.CurrentVersion is { } version)
                {
                    return version.ToString();
                }
            }
            catch (Exception)
            {
                // not Velopack-installed — fall back to the assembly version
            }

            return AssemblyVersion();
        }
    }

    // The informational version carries the semver pre-release suffix (e.g. "0.1.0-beta"), unlike
    // GetName().Version which is numeric-only. Strip any "+build" metadata the SDK appends.
    private static string AssemblyVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            int plus = informational.IndexOf('+', StringComparison.Ordinal);
            return plus >= 0 ? informational[..plus] : informational;
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    public async Task<Result<UpdateCheckResult>> CheckAsync(UpdateChannel channel, CancellationToken ct = default)
    {
        try
        {
            UpdateManager manager = CreateManager(channel);
            if (!manager.IsInstalled)
            {
                return Result.Success(new UpdateCheckResult(false, CurrentVersion, null));
            }

            _pending = await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            return _pending is null
                ? Result.Success(new UpdateCheckResult(false, CurrentVersion, null))
                : Result.Success(new UpdateCheckResult(true, CurrentVersion, _pending.TargetFullRelease.Version.ToString()));
        }
        catch (Exception ex)
        {
            return Result.Failure<UpdateCheckResult>("update.check_failed", ex.Message);
        }
    }

    public async Task<Result> DownloadAndApplyAsync(IProgress<int>? progress = null, CancellationToken ct = default)
    {
        try
        {
            UpdateManager manager = CreateManager(UpdateChannel.Stable);
            if (!manager.IsInstalled)
            {
                return Result.Failure("update.not_installed", "Updates apply to installed builds only.");
            }

            UpdateInfo? info = _pending ?? await manager.CheckForUpdatesAsync().ConfigureAwait(false);
            if (info is null)
            {
                return Result.Success();
            }

            await manager.DownloadUpdatesAsync(info, progress is null ? null : p => progress.Report(p), ct).ConfigureAwait(false);
            manager.ApplyUpdatesAndRestart(info);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure("update.apply_failed", ex.Message);
        }
    }

    private static UpdateManager CreateManager(UpdateChannel channel)
        => new(new GithubSource(RepositoryUrl, accessToken: null, prerelease: channel == UpdateChannel.Beta));
}

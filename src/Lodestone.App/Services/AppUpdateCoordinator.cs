using Lodestone.Application.Abstractions;
using Lodestone.Application.Common;
using Lodestone.Application.Messaging;
using Lodestone.Application.UseCases;
using Lodestone.Domain.Common;

namespace Lodestone.App.Services;

/// <summary>
/// Drives the app self-update flow end to end: check the entitled channel, quietly download any newer
/// build, then ask the user whether to restart now or let it apply the next time they close Lodestone.
/// Runs on app start (silently when nothing is found) and from the Settings "Check for updates" button
/// (which also reports "up to date"/errors). Single-flight, so a startup check and a manual click can't
/// overlap. Early-access (pre-release) builds are always labelled as such.
/// </summary>
public sealed class AppUpdateCoordinator
{
    // A short settle so a "restart now?" prompt never pops the instant the window appears.
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(3);

    private readonly CheckAppUpdateUseCase _check;
    private readonly IAppUpdater _updater;
    private readonly IDialogService _dialog;
    private readonly IMessageBus _bus;
    private int _running; // 0 = idle, 1 = a check/download is in flight

    public AppUpdateCoordinator(CheckAppUpdateUseCase check, IAppUpdater updater, IDialogService dialog, IMessageBus bus)
    {
        _check = check;
        _updater = updater;
        _dialog = dialog;
        _bus = bus;
    }

    /// <summary>Startup check: stays silent unless a newer build is found.</summary>
    public async Task CheckOnStartupAsync(CancellationToken ct = default)
    {
        await Task.Delay(StartupDelay, ct).ConfigureAwait(true);
        await RunAsync(announceNoUpdate: false, ct).ConfigureAwait(true);
    }

    /// <summary>Manual check (Settings button): also reports "up to date" and any errors.</summary>
    public Task CheckManuallyAsync(CancellationToken ct = default) => RunAsync(announceNoUpdate: true, ct);

    private async Task RunAsync(bool announceNoUpdate, CancellationToken ct)
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            if (announceNoUpdate)
            {
                Toast("Already checking", "An update check is already running.", ToastKind.Info);
            }

            return;
        }

        try
        {
            Result<UpdateCheckResult> checkResult = await _check.ExecuteAsync(ct).ConfigureAwait(true);
            if (checkResult.IsFailure)
            {
                if (announceNoUpdate)
                {
                    Toast("Couldn't check for updates", checkResult.Error.Message, ToastKind.Error);
                }

                return; // startup stays quiet on a transient network failure
            }

            UpdateCheckResult info = checkResult.Value;
            if (!info.UpdateAvailable)
            {
                if (announceNoUpdate)
                {
                    Toast("You're up to date", $"Lodestone {info.CurrentVersion} is the latest version.");
                }

                return;
            }

            bool early = info.IsPrerelease;
            Toast(
                early ? "Downloading early access update" : "Downloading update",
                $"Getting {Describe(info.LatestVersion)}…",
                ToastKind.Info);

            Result download = await _updater.DownloadAsync(ct: ct).ConfigureAwait(true);
            if (download.IsFailure)
            {
                if (announceNoUpdate)
                {
                    Toast("Update download failed", download.Error.Message, ToastKind.Error);
                }

                return;
            }

            PromptAndApply(info.LatestVersion, early);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }

    // Asks the user to restart now or update on next close. On this path we're back on the UI thread
    // (every await above is ConfigureAwait(true)), so the modal can be shown directly.
    private void PromptAndApply(string? version, bool early)
    {
        string name = Describe(version);
        string channelLine = early
            ? "\n\nThis is an early-access (beta) build — you're seeing it because you're a supporter with early access turned on."
            : string.Empty;

        bool restartNow = _dialog.Confirm(
            early ? "Early access update ready" : "Update ready",
            $"{name} has been downloaded and is ready to install.{channelLine}\n\n" +
            "Restart Lodestone now to finish updating? Choose No to update automatically the next time you close it.",
            warning: false);

        Result apply = restartNow ? _updater.ApplyAndRestart() : _updater.ApplyOnExit();
        if (apply.IsFailure)
        {
            Toast("Couldn't apply update", apply.Error.Message, ToastKind.Error);
            return;
        }

        // On "restart now" the process exits inside ApplyAndRestart, so this only runs for "Later".
        if (!restartNow)
        {
            Toast(
                early ? "Early access update scheduled" : "Update scheduled",
                $"{name} will be installed the next time you close Lodestone.");
        }
    }

    // "Lodestone 0.1.3 “Codename”" when we know the version (and any codename), else a safe fallback.
    private static string Describe(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return "a new version of Lodestone";
        }

        string? codename = ReleaseNames.For(version);
        return codename is null ? $"Lodestone {version}" : $"Lodestone {version} “{codename}”";
    }

    private void Toast(string title, string body, ToastKind kind = ToastKind.Success)
        => _bus.Publish(new ToastMessage(title, body, kind));
}

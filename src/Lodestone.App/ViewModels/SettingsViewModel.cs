using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Messaging;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.App.ViewModels;

/// <summary>The Settings screen — every control is wired to <see cref="LodestoneSettings"/> and saved on change.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly IDialogService _dialog;
    private readonly IGameLocator _locator;
    private readonly IAppUpdater _updater;
    private readonly IMessageBus _bus;
    private bool _ready;

    public SettingsViewModel(
        ISettingsStore settings,
        IDialogService dialog,
        IGameLocator locator,
        IAppUpdater updater,
        IMessageBus bus)
    {
        _settings = settings;
        _dialog = dialog;
        _locator = locator;
        _updater = updater;
        _bus = bus;
        ReloadFromSettings();
        AppVersionLabel = $"Lodestone {_updater.CurrentVersion}";
        _ready = true;
    }

    [ObservableProperty] private string? _gameDir;
    [ObservableProperty] private string _loader = "fabric";
    [ObservableProperty] private bool _autoUpdate;
    [ObservableProperty] private bool _notify;
    [ObservableProperty] private int _concurrent;
    [ObservableProperty] private bool _curseFallback;
    [ObservableProperty] private bool _closeToTray;
    [ObservableProperty] private string _appVersionLabel = "Lodestone";

    public void ReloadFromSettings()
    {
        LodestoneSettings s = _settings.Current;
        _gameDir = s.GameDirectory;
        _loader = s.DefaultLoader.ToSlug() is { Length: > 0 } slug ? slug : "fabric";
        _autoUpdate = s.AutoUpdate;
        _notify = s.NotifyUpdates;
        _concurrent = s.ConcurrentDownloads;
        _curseFallback = s.CurseForgeFallback;
        _closeToTray = s.CloseToTray;
        OnPropertyChanged(string.Empty); // refresh all bindings
    }

    partial void OnLoaderChanged(string value) => Save();
    partial void OnAutoUpdateChanged(bool value) => Save();
    partial void OnNotifyChanged(bool value) => Save();
    partial void OnConcurrentChanged(int value) => Save();
    partial void OnCurseFallbackChanged(bool value) => Save();
    partial void OnCloseToTrayChanged(bool value) => Save();
    partial void OnGameDirChanged(string? value) => Save();

    [RelayCommand]
    private void ChangeDir()
    {
        string? picked = _dialog.PickFolder(GameDir);
        if (picked is null)
        {
            return;
        }

        if (!_locator.IsValid(picked))
        {
            _bus.Publish(new ToastMessage("That doesn't look right", "Pick the folder that contains your mods/ and versions/ folders.", ToastKind.Warning));
            return;
        }

        GameDir = picked;
        _bus.Publish(new ToastMessage("Folder updated", "Game directory saved."));
    }

    [RelayCommand]
    private void DecreaseConcurrent() => Concurrent = Math.Max(LodestoneSettings.MinConcurrentDownloads, Concurrent - 1);

    [RelayCommand]
    private void IncreaseConcurrent() => Concurrent = Math.Min(LodestoneSettings.MaxConcurrentDownloads, Concurrent + 1);

    [RelayCommand]
    private async Task CheckUpdatesAsync()
    {
        Result<UpdateCheckResult> result = await _updater.CheckAsync(_settings.Current.UpdateChannel).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _bus.Publish(new ToastMessage("Couldn't check for updates", result.Error.Message, ToastKind.Error));
            return;
        }

        _bus.Publish(result.Value.UpdateAvailable
            ? new ToastMessage("Update available", $"Version {result.Value.LatestVersion} is ready to install.")
            : new ToastMessage("You're up to date", $"Lodestone {result.Value.CurrentVersion} is the latest version."));
    }

    private void Save()
    {
        if (!_ready)
        {
            return;
        }

        LodestoneSettings s = _settings.Current.Clone();
        s.GameDirectory = GameDir;
        s.DefaultLoader = Loader.ParseLoader();
        s.AutoUpdate = AutoUpdate;
        s.NotifyUpdates = Notify;
        s.ConcurrentDownloads = Concurrent;
        s.CurseForgeFallback = CurseFallback;
        s.CloseToTray = CloseToTray;
        _ = _settings.SaveAsync(s);
    }
}

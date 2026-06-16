using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Messaging;
using Lodestone.Application.Settings;
using Lodestone.Application.UseCases;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.App.ViewModels;

public sealed class RecentItemViewModel
{
    public RecentItemViewModel(InstalledContent item, string when)
    {
        Name = item.Name;
        AvatarLetter = char.ToUpperInvariant(item.Name.Length > 0 ? item.Name[0] : '?').ToString();
        TypeLabel = item.Type.ToDisplayName();
        When = when;
        Enabled = item.Enabled;
        EnabledLabel = item.Enabled ? "Enabled" : "Disabled";
    }

    public string Name { get; }
    public string AvatarLetter { get; }
    public string TypeLabel { get; }
    public string When { get; }
    public bool Enabled { get; }
    public string EnabledLabel { get; }
}

public sealed class UpdateRowViewModel
{
    public UpdateRowViewModel(InstalledContent item)
    {
        Name = item.Name;
        AvatarLetter = char.ToUpperInvariant(item.Name.Length > 0 ? item.Name[0] : '?').ToString();
        Label = $"v{item.Version}  →  latest";
    }

    public string Name { get; }
    public string AvatarLetter { get; }
    public string Label { get; }
}

/// <summary>The Home screen: stats, drag-and-drop install, recently added and available updates.</summary>
public sealed partial class HomeViewModel : ObservableObject
{
    private static readonly string[] WhenLabels = ["Just now", "2h ago", "Yesterday", "3d ago"];

    private readonly IInstalledContentRepository _repository;
    private readonly InstallLocalFileUseCase _installLocal;
    private readonly UpdateAllUseCase _updateAll;
    private readonly ISettingsStore _settings;
    private readonly IMessageBus _bus;
    private readonly IUiDispatcher _ui;
    private readonly IDialogService _dialog;

    public HomeViewModel(
        IInstalledContentRepository repository,
        InstallLocalFileUseCase installLocal,
        UpdateAllUseCase updateAll,
        ISettingsStore settings,
        IMessageBus bus,
        IUiDispatcher ui,
        IDialogService dialog)
    {
        _repository = repository;
        _installLocal = installLocal;
        _updateAll = updateAll;
        _settings = settings;
        _bus = bus;
        _ui = ui;
        _dialog = dialog;
        bus.Subscribe<LibraryChanged>(m => _ui.Post(() => _ = LoadAsync()));
    }

    [ObservableProperty] private int _modCount;
    [ObservableProperty] private int _packCount;
    [ObservableProperty] private int _shaderCount;
    [ObservableProperty] private string _activeVersion = "1.21.4";
    [ObservableProperty] private bool _hasUpdates;
    [ObservableProperty] private string _updatesLabel = string.Empty;

    [ObservableProperty] private bool _dragActive;
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private string _installName = string.Empty;
    [ObservableProperty] private string _installTypeLabel = string.Empty;

    public ObservableCollection<RecentItemViewModel> RecentItems { get; } = [];

    public ObservableCollection<UpdateRowViewModel> UpdateItems { get; } = [];

    public async Task LoadAsync()
    {
        IReadOnlyList<InstalledContent> all = await _repository.GetAllAsync().ConfigureAwait(true);

        ModCount = all.Count(i => i.Type == ContentType.Mod);
        PackCount = all.Count(i => i.Type == ContentType.ResourcePack);
        ShaderCount = all.Count(i => i.Type == ContentType.Shader);

        string selected = _settings.Current.SelectedVersion;
        ActiveVersion = selected is "all" or "" ? "1.21.4" : selected;

        RecentItems.Clear();
        int index = 0;
        foreach (InstalledContent item in all.Take(4))
        {
            RecentItems.Add(new RecentItemViewModel(item, WhenLabels[Math.Min(index, WhenLabels.Length - 1)]));
            index++;
        }

        UpdateItems.Clear();
        foreach (InstalledContent item in all.Where(i => i.UpdateAvailable))
        {
            UpdateItems.Add(new UpdateRowViewModel(item));
        }

        HasUpdates = UpdateItems.Count > 0;
        UpdatesLabel = $"{UpdateItems.Count} update{(UpdateItems.Count == 1 ? string.Empty : "s")} available";
    }

    /// <summary>Installs a batch of dropped/picked files into the active version.</summary>
    public async Task HandleFilesAsync(IReadOnlyList<string> paths)
    {
        if (paths.Count == 0)
        {
            return;
        }

        GameVersion target = ResolveTargetVersion();

        foreach (string path in paths)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            IsInstalling = true;
            InstallName = name;
            InstallTypeLabel = "Reading…";

            Result<InstalledContent> result = await _installLocal.ExecuteAsync(path, target).ConfigureAwait(true);

            if (result.IsSuccess)
            {
                _bus.Publish(new ToastMessage("Installed", $"{result.Value.Name} · {result.Value.Type.ToDisplayName()}"));
            }
            else
            {
                _bus.Publish(new ToastMessage("Couldn't install", result.Error.Message, ToastKind.Error));
            }
        }

        IsInstalling = false;
        _bus.Publish(new LibraryChanged());
    }

    [RelayCommand]
    private void BrowseFiles()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "Mods, packs & shaders|*.jar;*.zip;*.litemod;*.mcpack|All files|*.*",
            Title = "Choose files to install",
        };

        if (dialog.ShowDialog() == true)
        {
            _ = HandleFilesAsync(dialog.FileNames);
        }
    }

    [RelayCommand]
    private async Task UpdateAllAsync()
    {
        Result<int> result = await _updateAll.ExecuteAsync(ResolveTargetVersion()).ConfigureAwait(true);
        if (result.IsSuccess && result.Value > 0)
        {
            _bus.Publish(new ToastMessage("Updated", $"{result.Value} mod{(result.Value == 1 ? string.Empty : "s")} updated to the latest version"));
            _bus.Publish(new LibraryChanged());
        }
    }

    private GameVersion ResolveTargetVersion()
    {
        string selected = _settings.Current.SelectedVersion;
        return GameVersion.Parse(selected is "all" or "" ? "1.21.4" : selected);
    }
}

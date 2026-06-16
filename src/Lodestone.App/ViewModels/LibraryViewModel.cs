using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lodestone.App.Services;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Compatibility;
using Lodestone.Application.Library;
using Lodestone.Application.Messaging;
using Lodestone.Application.Settings;
using Lodestone.Application.UseCases;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using Lodestone.Domain.Compatibility;

namespace Lodestone.App.ViewModels;

/// <summary>"My Content": per-version profiles, type tabs, search, toggle/uninstall, and the
/// compatibility symbols produced by the rule engine.</summary>
public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly IInstalledContentRepository _repository;
    private readonly ICompatibilityService _compatibility;
    private readonly ToggleContentUseCase _toggle;
    private readonly UninstallContentUseCase _uninstall;
    private readonly ISettingsStore _settings;
    private readonly IMessageBus _bus;
    private readonly IUiDispatcher _ui;

    private IReadOnlyList<InstalledContent> _all = [];
    private IReadOnlyDictionary<string, CompatibilityReport> _reports = new Dictionary<string, CompatibilityReport>();

    public LibraryViewModel(
        IInstalledContentRepository repository,
        ICompatibilityService compatibility,
        ToggleContentUseCase toggle,
        UninstallContentUseCase uninstall,
        ISettingsStore settings,
        IMessageBus bus,
        IUiDispatcher ui)
    {
        _repository = repository;
        _compatibility = compatibility;
        _toggle = toggle;
        _uninstall = uninstall;
        _settings = settings;
        _bus = bus;
        _ui = ui;
        _mcVersion = settings.Current.SelectedVersion;
        bus.Subscribe<LibraryChanged>(m => _ui.Post(() => _ = LoadAsync()));
    }

    public ObservableCollection<string> Versions { get; } = ["all", "1.21.4", "1.21.1", "1.20.1", "1.19.2"];

    public ObservableCollection<ContentItemViewModel> Items { get; } = [];

    [ObservableProperty] private string _libTab = "mods";
    [ObservableProperty] private string _mcVersion;
    [ObservableProperty] private string _libSearch = string.Empty;
    [ObservableProperty] private string _countLabel = string.Empty;
    [ObservableProperty] private bool _isEmpty;

    partial void OnLibTabChanged(string value) => Rebuild();

    partial void OnLibSearchChanged(string value) => Rebuild();

    partial void OnMcVersionChanged(string value)
    {
        LodestoneSettings updated = _settings.Current.Clone();
        updated.SelectedVersion = value;
        _ = _settings.SaveAsync(updated);
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        _all = await _repository.GetAllAsync().ConfigureAwait(true);

        GameVersion? activeVersion = _mcVersion is "all" or ""
            ? null
            : GameVersion.Create(_mcVersion).Match<GameVersion?>(v => v, _ => null);

        _reports = _compatibility.Analyze(new CompatibilityContext(_all, activeVersion, _settings.Current.DefaultLoader));
        Rebuild();
    }

    private void Rebuild()
    {
        ContentType type = _libTab switch
        {
            "resourcepacks" => ContentType.ResourcePack,
            "shaders" => ContentType.Shader,
            _ => ContentType.Mod,
        };

        bool allVersions = _mcVersion is "all" or "";
        GameVersion? version = allVersions ? null : GameVersion.Create(_mcVersion).Match<GameVersion?>(v => v, _ => null);

        var filter = new LibraryFilter(type, version, string.IsNullOrWhiteSpace(LibSearch) ? null : LibSearch);
        IReadOnlyList<InstalledContent> filtered = LibraryQuery.Apply(_all, filter);

        Items.Clear();
        foreach (InstalledContent item in filtered)
        {
            _reports.TryGetValue(item.Id, out CompatibilityReport? report);
            Items.Add(new ContentItemViewModel(item, report, allVersions, ToggleAsync, UninstallAsync));
        }

        string tabLabel = _libTab switch
        {
            "resourcepacks" => "resource packs",
            "shaders" => "shaders",
            _ => "mods",
        };
        CountLabel = $"{Items.Count} {tabLabel}" + (allVersions ? string.Empty : $"   ·   {_mcVersion}");
        IsEmpty = Items.Count == 0;
    }

    private async Task ToggleAsync(string id)
    {
        Result result = await _toggle.ExecuteAsync(id).ConfigureAwait(true);
        if (result.IsFailure)
        {
            _bus.Publish(new ToastMessage("Couldn't change that", result.Error.Message, ToastKind.Error));
        }

        _bus.Publish(new LibraryChanged());
    }

    private async Task UninstallAsync(string id)
    {
        InstalledContent? item = await _repository.FindAsync(id).ConfigureAwait(true);
        Result result = await _uninstall.ExecuteAsync(id).ConfigureAwait(true);
        if (result.IsSuccess && item is not null)
        {
            _bus.Publish(new ToastMessage("Uninstalled", item.Name));
        }
        else if (result.IsFailure)
        {
            _bus.Publish(new ToastMessage("Couldn't uninstall", result.Error.Message, ToastKind.Error));
        }

        _bus.Publish(new LibraryChanged());
    }
}

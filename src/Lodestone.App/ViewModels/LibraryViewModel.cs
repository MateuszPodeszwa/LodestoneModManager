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
    private readonly IGameInventory _inventory;

    private IReadOnlyList<InstalledContent> _all = [];
    private IReadOnlyList<GameVersion> _installedVersions = [];
    private IReadOnlyDictionary<string, CompatibilityReport> _reports = new Dictionary<string, CompatibilityReport>();
    private bool _suppressReload;

    public LibraryViewModel(
        IInstalledContentRepository repository,
        ICompatibilityService compatibility,
        ToggleContentUseCase toggle,
        UninstallContentUseCase uninstall,
        ISettingsStore settings,
        IMessageBus bus,
        IUiDispatcher ui,
        IGameInventory inventory)
    {
        _repository = repository;
        _compatibility = compatibility;
        _toggle = toggle;
        _uninstall = uninstall;
        _settings = settings;
        _bus = bus;
        _ui = ui;
        _inventory = inventory;
        _mcVersion = settings.Current.SelectedVersion;
        bus.Subscribe<LibraryChanged>(m => _ui.Post(() => _ = LoadAsync()));
    }

    /// <summary>The version filter options: "All versions" plus whatever is actually installed (newest first).</summary>
    public ObservableCollection<string> Versions { get; } = ["all"];

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
        // A transient null/empty can arrive while the dropdown's ItemsSource is being rebuilt — ignore it.
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        LodestoneSettings updated = _settings.Current.Clone();
        updated.SelectedVersion = value;
        _ = _settings.SaveAsync(updated);

        if (!_suppressReload)
        {
            _ = LoadAsync();
        }
    }

    public async Task LoadAsync()
    {
        _all = await _repository.GetAllAsync().ConfigureAwait(true);
        RefreshVersionOptions();

        GameVersion? activeVersion = ActiveProfile.Selected(_settings.Current);

        _reports = _compatibility.Analyze(new CompatibilityContext(_all, activeVersion, _settings.Current.DefaultLoader)
        {
            InstalledGameVersions = _installedVersions,
        });
        Rebuild();
    }

    // Rebuilds the dropdown from the installed versions and repairs a stale stored selection — so the
    // list only ever offers versions the user actually has, never the old hardcoded set.
    private void RefreshVersionOptions()
    {
        _installedVersions = _inventory.InstalledVersions();

        var desired = new List<string> { "all" };
        desired.AddRange(_installedVersions.Select(v => v.Value));

        bool installedSelection =
            _installedVersions.Any(v => v.Value.Equals(_mcVersion, StringComparison.OrdinalIgnoreCase));
        string target = _mcVersion == "all" || installedSelection
            ? _mcVersion
            : _installedVersions.Count > 0 ? _installedVersions[0].Value : "all";

        _suppressReload = true;
        try
        {
            if (!Versions.SequenceEqual(desired, StringComparer.OrdinalIgnoreCase))
            {
                Versions.Clear();
                foreach (string version in desired)
                {
                    Versions.Add(version);
                }
            }

            // Re-assert the selection (the rebuild above can clear the bound SelectedItem) and persist
            // a repaired value when the previous selection is no longer installed.
            McVersion = target;
        }
        finally
        {
            _suppressReload = false;
        }
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

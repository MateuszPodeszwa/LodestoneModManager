using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lodestone.App.Services;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Messaging;
using Lodestone.Application.Settings;
using Lodestone.Application.UseCases;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.App.ViewModels;

/// <summary>The Browse screen: faceted Modrinth search with debounced input and one-click install.</summary>
public sealed partial class BrowseViewModel : ObservableObject
{
    private readonly IModSourceRegistry _registry;
    private readonly InstallFromCatalogUseCase _install;
    private readonly IInstalledContentRepository _repository;
    private readonly ISettingsStore _settings;
    private readonly IMessageBus _bus;
    private readonly IUiDispatcher _ui;

    private CancellationTokenSource? _debounce;
    private bool _loadedOnce;

    public BrowseViewModel(
        IModSourceRegistry registry,
        InstallFromCatalogUseCase install,
        IInstalledContentRepository repository,
        ISettingsStore settings,
        IMessageBus bus,
        IUiDispatcher ui)
    {
        _registry = registry;
        _install = install;
        _repository = repository;
        _settings = settings;
        _bus = bus;
        _ui = ui;
        bus.Subscribe<LibraryChanged>(_ => _ui.Post(MarkInstalledFromLibrary));
    }

    /// <summary>Set by the shell so cards can open the detail modal.</summary>
    public Action<CatalogProject>? OpenDetailRequested { get; set; }

    public ObservableCollection<CatalogItemViewModel> Results { get; } = [];

    [ObservableProperty] private string _browseSource = "modrinth";
    [ObservableProperty] private string _browseQuery = string.Empty;
    [ObservableProperty] private string _browseSort = "relevance";
    [ObservableProperty] private string _browseCat = "all";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isEmpty;
    [ObservableProperty] private string _resultCountLabel = string.Empty;

    partial void OnBrowseSourceChanged(string value) => QueueSearch();
    partial void OnBrowseQueryChanged(string value) => QueueSearch();
    partial void OnBrowseSortChanged(string value) => QueueSearch();
    partial void OnBrowseCatChanged(string value) => QueueSearch();

    public async Task EnsureLoadedAsync()
    {
        if (_loadedOnce)
        {
            return;
        }

        _loadedOnce = true;
        await SearchAsync(CancellationToken.None).ConfigureAwait(true);
    }

    private void QueueSearch()
    {
        _debounce?.Cancel();
        _debounce = new CancellationTokenSource();
        CancellationToken token = _debounce.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token).ConfigureAwait(true);
                _ui.Post(() => _ = SearchAsync(token));
            }
            catch (TaskCanceledException)
            {
                // superseded by a newer keystroke
            }
        }, token);
    }

    private async Task SearchAsync(CancellationToken ct)
    {
        IModSource? source = _registry.Find(BrowseSource);
        if (source is null || !source.IsConfigured)
        {
            Results.Clear();
            IsEmpty = true;
            ResultCountLabel = BrowseSource == "curseforge" ? "CurseForge isn't configured yet" : "0 results";
            return;
        }

        IsLoading = true;
        try
        {
            ModSearchQuery query = BuildQuery();
            Result<IReadOnlyList<CatalogProject>> result = await source.SearchAsync(query, ct).ConfigureAwait(true);
            if (ct.IsCancellationRequested)
            {
                return;
            }

            Results.Clear();
            if (result.IsSuccess)
            {
                HashSet<string> installed = (await _repository.GetAllAsync(ct).ConfigureAwait(true))
                    .Select(i => i.Id)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (CatalogProject project in result.Value)
                {
                    Results.Add(new CatalogItemViewModel(project, installed.Contains(project.Id), InstallAsync, p => OpenDetailRequested?.Invoke(p)));
                }

                ResultCountLabel = $"{Results.Count} result{(Results.Count == 1 ? string.Empty : "s")}";
            }
            else
            {
                ResultCountLabel = "Couldn't reach " + source.Name;
            }

            IsEmpty = Results.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private ModSearchQuery BuildQuery()
    {
        ContentType? type = BrowseCat switch
        {
            "resource-packs" => ContentType.ResourcePack,
            "shaders" => ContentType.Shader,
            _ => null,
        };

        string? category = BrowseCat switch
        {
            "all" or "resource-packs" or "shaders" => null,
            "tech" => "technology",
            _ => BrowseCat,
        };

        ModSortOrder sort = BrowseSort switch
        {
            "downloads" => ModSortOrder.Downloads,
            "followers" => ModSortOrder.Followers,
            _ => ModSortOrder.Relevance,
        };

        return new ModSearchQuery(BrowseQuery, type, category, sort, Limit: 30);
    }

    private async Task InstallAsync(CatalogItemViewModel item)
    {
        item.Installing = true;
        try
        {
            GameVersion target = ResolveTargetVersion();
            Result<InstalledContent> result = await _install
                .ExecuteAsync(item.Project, target, _settings.Current.DefaultLoader)
                .ConfigureAwait(true);

            if (result.IsSuccess)
            {
                item.Installed = true;
                _bus.Publish(new ToastMessage("Installed", $"{item.Name} · {result.Value.Type.ToDisplayName()}"));
                _bus.Publish(new LibraryChanged());
            }
            else
            {
                _bus.Publish(new ToastMessage("Couldn't install", result.Error.Message, ToastKind.Error));
            }
        }
        finally
        {
            item.Installing = false;
        }
    }

    private async void MarkInstalledFromLibrary()
    {
        HashSet<string> installed = (await _repository.GetAllAsync().ConfigureAwait(true))
            .Select(i => i.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (CatalogItemViewModel item in Results)
        {
            item.Installed = installed.Contains(item.Project.Id);
        }
    }

    private GameVersion ResolveTargetVersion()
    {
        string selected = _settings.Current.SelectedVersion;
        return GameVersion.Parse(selected is "all" or "" ? "1.21.4" : selected);
    }
}

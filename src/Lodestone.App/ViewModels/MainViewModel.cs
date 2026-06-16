using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lodestone.App.Services;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Messaging;
using Lodestone.Application.Settings;
using Lodestone.Application.Supporter;
using Lodestone.Application.UseCases;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.App.ViewModels;

/// <summary>The shell: navigation, the detail modal, onboarding overlay, toasts and startup wiring.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ISettingsStore _settings;
    private readonly IEntitlementStore _entitlements;
    private readonly SupporterService _supporter;
    private readonly IMessageBus _bus;
    private readonly IUiDispatcher _ui;
    private readonly InstallFromCatalogUseCase _install;
    private readonly IInstalledContentRepository _repository;
    private readonly RefreshUpdatesUseCase _refresh;
    private readonly IGameLocator _locator;
    private readonly IDialogService _dialog;
    private readonly ReconcileLibraryUseCase _reconcile;
    private readonly ILoaderInstaller _loaderInstaller;
    private readonly IModSourceRegistry _registry;

    public MainViewModel(
        HomeViewModel home,
        LibraryViewModel library,
        BrowseViewModel browse,
        SettingsViewModel settings,
        DonateViewModel donate,
        OnboardingViewModel onboarding,
        ToastsViewModel toasts,
        ISettingsStore settingsStore,
        IEntitlementStore entitlements,
        SupporterService supporter,
        IMessageBus bus,
        IUiDispatcher ui,
        InstallFromCatalogUseCase install,
        IInstalledContentRepository repository,
        RefreshUpdatesUseCase refresh,
        IGameLocator locator,
        IDialogService dialog,
        ReconcileLibraryUseCase reconcile,
        ILoaderInstaller loaderInstaller,
        IModSourceRegistry registry)
    {
        Home = home;
        Library = library;
        Browse = browse;
        Settings = settings;
        Donate = donate;
        Onboarding = onboarding;
        Toasts = toasts;
        _settings = settingsStore;
        _entitlements = entitlements;
        _supporter = supporter;
        _bus = bus;
        _ui = ui;
        _install = install;
        _repository = repository;
        _refresh = refresh;
        _locator = locator;
        _dialog = dialog;
        _reconcile = reconcile;
        _loaderInstaller = loaderInstaller;
        _registry = registry;

        Browse.OpenDetailRequested = OpenDetail;
        Onboarding.Completed += OnOnboardingCompleted;
        _entitlements.Changed += (_, _) => _ui.Post(RefreshSupporter);
        _settings.Changed += (_, _) => _ui.Post(() => OnPropertyChanged(nameof(IsGameReady)));
        _currentScreen = home;
    }

    public HomeViewModel Home { get; }
    public LibraryViewModel Library { get; }
    public BrowseViewModel Browse { get; }
    public SettingsViewModel Settings { get; }
    public DonateViewModel Donate { get; }
    public OnboardingViewModel Onboarding { get; }
    public ToastsViewModel Toasts { get; }

    [ObservableProperty] private object _currentScreen;
    [ObservableProperty] private string _route = "home";
    [ObservableProperty] private bool _showOnboarding;
    [ObservableProperty] private DetailViewModel? _currentDetail;

    public bool IsSupporter => _supporter.IsSupporter;

    /// <summary>True once a valid Minecraft folder is configured; gates all install actions.</summary>
    public bool IsGameReady => _locator.IsValid(_settings.Current.GameDirectory);

    public bool IsModalOpen => CurrentDetail is not null;

    partial void OnCurrentDetailChanged(DetailViewModel? value) => OnPropertyChanged(nameof(IsModalOpen));

    public async Task InitializeAsync()
    {
        ShowOnboarding = !_settings.Current.OnboardingCompleted;
        RefreshSupporter();
        OnPropertyChanged(nameof(IsGameReady));

        // Auto-discovery: import any mods already sitting in the game folders before showing the library.
        if (IsGameReady)
        {
            await _reconcile.ExecuteAsync(ActiveVersion() ?? GameVersion.Parse("1.21.4")).ConfigureAwait(true);
        }

        // Local state loads fast; await it so the first screen is populated.
        await Home.LoadAsync().ConfigureAwait(true);
        await Library.LoadAsync().ConfigureAwait(true);

        // Network-backed work is fire-and-forget so the shell never blocks on it.
        _ = Browse.EnsureLoadedAsync();

        // Per spec: the mod updater runs on app start (and on manual refresh) — never on a timer.
        _ = RunStartupRefreshAsync();

        // Make sure the configured loader is actually installed (Fabric/Quilt), quietly, on start.
        if (IsGameReady)
        {
            _ = _loaderInstaller.EnsureInstalledAsync(_settings.Current.DefaultLoader, ActiveVersion() ?? GameVersion.Parse("1.21.4"));
        }
    }

    [RelayCommand]
    private async Task LocateGameAsync()
    {
        string? picked = _dialog.PickFolder(_settings.Current.GameDirectory);
        if (picked is null)
        {
            return;
        }

        if (!_locator.IsValid(picked))
        {
            _bus.Publish(new ToastMessage("That doesn't look right", "Pick the folder that holds your mods/ and versions/.", ToastKind.Warning));
            return;
        }

        LodestoneSettings s = _settings.Current.Clone();
        s.GameDirectory = picked;
        await _settings.SaveAsync(s).ConfigureAwait(true);
        OnPropertyChanged(nameof(IsGameReady));
        _bus.Publish(new ToastMessage("Minecraft folder set", picked));

        GameVersion version = ActiveVersion() ?? GameVersion.Parse("1.21.4");
        await _reconcile.ExecuteAsync(version).ConfigureAwait(true);
        _ = _loaderInstaller.EnsureInstalledAsync(_settings.Current.DefaultLoader, version);
        _bus.Publish(new LibraryChanged());
    }

    [RelayCommand] private void GoHome() => Navigate("home", Home);
    [RelayCommand] private void GoLibrary() => Navigate("library", Library);
    [RelayCommand] private void GoBrowse() => Navigate("browse", Browse);
    [RelayCommand] private void GoDonate() => Navigate("donate", Donate);
    [RelayCommand] private void GoSettings() => Navigate("settings", Settings);

    private void Navigate(string route, object screen)
    {
        Route = route;
        CurrentScreen = screen;
        CurrentDetail = null;
    }

    private async Task RunStartupRefreshAsync()
    {
        Result<UpdateSummary> result = await _refresh.ExecuteAsync(ActiveVersion()).ConfigureAwait(true);
        if (result.IsSuccess && (result.Value.UpdatesAvailable > 0 || result.Value.Updated > 0))
        {
            _bus.Publish(new LibraryChanged());
        }
    }

    private async void OpenDetail(CatalogProject project)
    {
        bool installed = await _repository.FindAsync(project.Id).ConfigureAwait(true) is not null;
        var detail = new DetailViewModel(project, installed, InstallFromDetailAsync, () => CurrentDetail = null);
        CurrentDetail = detail;

        // Enrich with the full project (long description + screenshot gallery) once it loads.
        IModSource source = _registry.Find(project.Source) ?? _registry.Primary;
        Result<CatalogProject> full = await source.GetProjectAsync(project.Id).ConfigureAwait(true);
        if (full.IsSuccess && ReferenceEquals(CurrentDetail, detail))
        {
            CatalogProject merged = project with { Body = full.Value.Body, GalleryUrls = full.Value.GalleryUrls };
            CurrentDetail = new DetailViewModel(merged, installed, InstallFromDetailAsync, () => CurrentDetail = null);
        }
    }

    private async Task InstallFromDetailAsync(DetailViewModel detail)
    {
        if (!IsGameReady)
        {
            _bus.Publish(new ToastMessage("Set your Minecraft folder first", "Lodestone needs to know where to install. Use “Locate Minecraft”.", ToastKind.Warning));
            return;
        }

        detail.Installing = true;
        var progress = new Progress<TransferProgress>(p =>
            _ui.Post(() => detail.InstallPercent = (int)Math.Round((p.Fraction ?? 0) * 100)));

        Result<InstalledContent> result = await _install
            .ExecuteAsync(detail.Project, ActiveVersion() ?? GameVersion.Parse("1.21.4"), _settings.Current.DefaultLoader, progress)
            .ConfigureAwait(true);

        detail.Installing = false;
        if (result.IsSuccess)
        {
            detail.Installed = true;
            _bus.Publish(new ToastMessage("Installed", $"{detail.Name} · {result.Value.Type.ToDisplayName()}"));
            _bus.Publish(new LibraryChanged());
        }
        else
        {
            _bus.Publish(new ToastMessage("Couldn't install", result.Error.Message, ToastKind.Error));
        }
    }

    private void OnOnboardingCompleted()
    {
        ShowOnboarding = false;
        _bus.Publish(new ToastMessage("Welcome to Lodestone", "Drag any mod, pack or shader here to install it"));
        _bus.Publish(new LibraryChanged());
    }

    private void RefreshSupporter() => OnPropertyChanged(nameof(IsSupporter));

    private GameVersion? ActiveVersion()
    {
        string selected = _settings.Current.SelectedVersion;
        return selected is "all" or "" ? null : GameVersion.Create(selected).Match<GameVersion?>(v => v, _ => null);
    }
}

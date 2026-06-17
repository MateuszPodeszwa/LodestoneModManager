using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Lodestone.App.Services;
using Lodestone.App.ViewModels;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Messaging;
using Lodestone.Application.Settings;
using Lodestone.Application.Supporter;
using Lodestone.Application.UseCases;
using Lodestone.Infrastructure.DependencyInjection;
using Lodestone.Infrastructure.Diagnostics;
using Lodestone.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Lodestone.App;

/// <summary>
/// WPF composition root. Builds the service provider (core + infrastructure + UI), loads persisted
/// state before any view model is constructed, then shows the shell. There is no background host or
/// timer — everything is request/refresh driven.
/// </summary>
public partial class App : System.Windows.Application
{
    private static readonly string SmokeLogPath = Path.Combine(Path.GetTempPath(), "lodestone-smoke.log");

    private ServiceProvider? _provider;
    private IDisposable? _diagnostics;
    private bool _smoke;
    private bool _smokeError;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _smoke = Environment.GetEnvironmentVariable("LODESTONE_SMOKE") == "1";
        DispatcherUnhandledException += OnUnhandledException;

        var services = new ServiceCollection();
        services.AddLodestone();

        // UI-layer services
        services.AddSingleton<IUiDispatcher, UiDispatcher>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IAppUpdater, VelopackAppUpdater>();
        // App self-update: the channel gate lives in the Application layer (testable); the coordinator
        // drives check → download → restart-prompt and is shared by startup and the Settings button.
        services.AddTransient<CheckAppUpdateUseCase>();
        services.AddSingleton<AppUpdateCoordinator>();
        services.AddSingleton<OperationGate>();

        // View models
        services.AddSingleton<HomeViewModel>();
        services.AddSingleton<LibraryViewModel>();
        services.AddSingleton<BrowseViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<DonateViewModel>();
        services.AddSingleton<OnboardingViewModel>();
        services.AddSingleton<ToastsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        _provider = services.BuildServiceProvider();

        // Load persisted state up front so view models read real values in their constructors.
        ISettingsStore settingsStore = _provider.GetRequiredService<ISettingsStore>();
        await settingsStore.LoadAsync();
        await _provider.GetRequiredService<IEntitlementStore>().LoadAsync();

        // Apply the saved accent (a supporter perk) before any view loads, so it renders themed from the
        // first frame. AccentApplier ignores a custom accent when the user isn't a supporter.
        AccentApplier.Apply(settingsStore.Current.AccentColor, _provider.GetRequiredService<SupporterService>().IsSupporter);

        // Diagnostics: outside smoke runs, mirror every toast into the log and write a startup banner, so
        // the logs folder (what the "Open logs" button and the FAQ point users to) actually has the context
        // needed for a bug report. Done before the window shows so the first toast is already captured.
        if (!_smoke)
        {
            _diagnostics = DiagnosticLogger.Attach(_provider.GetRequiredService<IMessageBus>());
            LogStartupBanner(settingsStore);
        }

        var main = _provider.GetRequiredService<MainViewModel>();
        var window = _provider.GetRequiredService<MainWindow>();
        window.DataContext = main;
        MainWindow = window;
        window.Show();

        if (_smoke)
        {
            TryDelete(SmokeLogPath);
            StartSmokeWatchdog(main);
        }

        try
        {
            await main.InitializeAsync();
        }
        catch (Exception ex) when (_smoke)
        {
            LogSmoke(ex);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (!_smoke)
        {
            LodestoneLog.Info("Lodestone exiting");
        }

        _diagnostics?.Dispose();
        _provider?.Dispose();
        base.OnExit(e);
    }

    // One-time banner with the essentials for a bug report: app version, OS, runtime, and whether a valid
    // Minecraft folder is configured. Best-effort — diagnostics must never break startup.
    private void LogStartupBanner(ISettingsStore settings)
    {
        ServiceProvider provider = _provider!; // non-null here: the banner is only logged after the provider is built
        try
        {
            string version = provider.GetRequiredService<IAppUpdater>().CurrentVersion;
            LodestoneLog.Info($"Lodestone {version} starting — {RuntimeInformation.OSDescription} — {RuntimeInformation.FrameworkDescription}");

            string? dir = settings.Current.GameDirectory;
            string state = string.IsNullOrWhiteSpace(dir)
                ? "not set"
                : provider.GetRequiredService<IGameLocator>().IsValid(dir) ? "set (valid)" : "set (invalid)";
            LodestoneLog.Info($"Game directory: {state}");
        }
        catch (Exception ex)
        {
            LodestoneLog.Error("Failed to write startup banner", ex); // never let diagnostics break startup
        }
    }

    // Renders every screen once (catching runtime resource/binding errors) then exits. Runs
    // independently of InitializeAsync so a slow/failed network load can't stall the check.
    private void StartSmokeWatchdog(MainViewModel main)
    {
        var steps = new Queue<Action>(
        [
            () => main.GoLibraryCommand.Execute(null),
            () => main.GoBrowseCommand.Execute(null),
            () => main.OpenSampleDetailForSmoke(),
            () => main.GoSettingsCommand.Execute(null),
            () => main.GoDonateCommand.Execute(null),
            () => main.GoHomeCommand.Execute(null),
        ]);

        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        timer.Tick += (_, _) =>
        {
            try
            {
                if (steps.Count > 0)
                {
                    steps.Dequeue()();
                    return;
                }
            }
            catch (Exception ex)
            {
                LogSmoke(ex);
            }

            timer.Stop();
            Shutdown(_smokeError ? 1 : 0);
        };
        timer.Start();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        if (_smoke)
        {
            LogSmoke(e.Exception);
            e.Handled = true;
            return;
        }

        Lodestone.Infrastructure.Persistence.LodestoneLog.Error("Unhandled UI exception", e.Exception);
        MessageBox.Show(
            $"Something went wrong:\n\n{e.Exception.Message}\n\nDetails were logged to %AppData%\\Lodestone\\logs.",
            "Lodestone",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        e.Handled = true;
    }

    private void LogSmoke(Exception ex)
    {
        _smokeError = true;
        try
        {
            File.AppendAllText(SmokeLogPath, ex + Environment.NewLine + new string('-', 60) + Environment.NewLine);
        }
        catch (IOException)
        {
            // ignore
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
            // ignore
        }
    }
}

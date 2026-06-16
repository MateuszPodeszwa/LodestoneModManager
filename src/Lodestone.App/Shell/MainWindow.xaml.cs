using System.ComponentModel;
using System.Windows;
using Lodestone.Application.Messaging;
using Lodestone.Application.Settings;

namespace Lodestone.App;

/// <summary>The custom-chrome shell window. Caption buttons and close-to-tray live here.</summary>
public partial class MainWindow : Window
{
    private readonly ISettingsStore _settings;
    private readonly IMessageBus _bus;

    public MainWindow(ISettingsStore settings, IMessageBus bus)
    {
        _settings = settings;
        _bus = bus;
        InitializeComponent();
        StateChanged += OnStateChanged;
    }

    private void OnMinimize(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestore(object sender, RoutedEventArgs e)
        => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnClose(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosing(CancelEventArgs e)
    {
        // Honour "close to tray": keep the process alive (minimized) instead of exiting. By default
        // this is OFF, so closing fully ends the process (see docs/RISK-ANALYSIS.md §7).
        if (_settings.Current.CloseToTray)
        {
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            _bus.Publish(new ToastMessage("Still running", "Lodestone is minimized and keeps running in the background."));
            return;
        }

        base.OnClosing(e);
    }

    // Stop the maximized window from spilling under the taskbar (a WindowChrome quirk).
    private void OnStateChanged(object? sender, EventArgs e)
        => RootGrid.Margin = WindowState == WindowState.Maximized ? new Thickness(8) : default;
}

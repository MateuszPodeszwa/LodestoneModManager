using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Lodestone.App.Services;

/// <summary>Marshals work onto the WPF UI thread.</summary>
public interface IUiDispatcher
{
    void Post(Action action);
}

/// <summary>Default <see cref="IUiDispatcher"/> backed by the WPF <see cref="Dispatcher"/>; runs the
/// action inline when already on the UI thread, otherwise marshals onto it.</summary>
public sealed class UiDispatcher : IUiDispatcher
{
    private readonly Dispatcher _dispatcher = System.Windows.Application.Current.Dispatcher;

    public void Post(Action action)
    {
        if (_dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            _dispatcher.Invoke(action);
        }
    }
}

/// <summary>Native dialogs and shell integration kept behind an interface so view models stay testable.</summary>
public interface IDialogService
{
    string? PickFolder(string? initialDirectory);

    void OpenUrl(string url);

    /// <summary>Modal yes/no confirmation; returns true only when the user explicitly confirms.</summary>
    bool Confirm(string title, string message);
}

/// <summary>Default <see cref="IDialogService"/> using native Win32 dialogs and the OS shell to open URLs.</summary>
public sealed class DialogService : IDialogService
{
    public string? PickFolder(string? initialDirectory)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select your Minecraft folder",
            InitialDirectory = initialDirectory ?? string.Empty,
        };

        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }

    public void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            MessageBox.Show($"Couldn't open the link:\n{url}", "Lodestone", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public bool Confirm(string title, string message)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}

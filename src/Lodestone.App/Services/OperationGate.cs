using CommunityToolkit.Mvvm.ComponentModel;

namespace Lodestone.App.Services;

/// <summary>
/// App-wide single-flight gate for disk-mutating operations — loader install/update, profile switch,
/// content install, reset. While one runs, <see cref="IsBusy"/> is true so the UI can disable the
/// loader/version controls and install buttons. This prevents overlapping operations that would race on
/// the same files (mods/, versions/, launcher_profiles.json) and spam errors.
/// </summary>
public sealed partial class OperationGate : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsIdle))]
    private bool _isBusy;

    /// <summary>A short description of the in-flight operation, shown by the global activity bar.</summary>
    [ObservableProperty]
    private string _statusLabel = "Working…";

    /// <summary>The inverse of <see cref="IsBusy"/>, for binding a control's <c>IsEnabled</c>.</summary>
    public bool IsIdle => !IsBusy;

    /// <summary>Runs <paramref name="operation"/> exclusively with a generic label (see the labelled overload).</summary>
    public Task<bool> RunAsync(Func<Task> operation) => RunAsync("Working…", operation);

    /// <summary>
    /// Runs <paramref name="operation"/> exclusively, surfacing <paramref name="label"/> on the activity bar
    /// while it runs. If another operation is already in flight this does nothing and returns <c>false</c>, so
    /// the caller can surface a "please wait" message instead of starting a second, racing operation.
    /// </summary>
    public async Task<bool> RunAsync(string label, Func<Task> operation)
    {
        if (IsBusy)
        {
            return false;
        }

        StatusLabel = label;
        IsBusy = true;
        try
        {
            await operation();
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }
}

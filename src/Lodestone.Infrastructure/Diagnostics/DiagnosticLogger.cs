using Lodestone.Application.Messaging;
using Lodestone.Infrastructure.Persistence;

namespace Lodestone.Infrastructure.Diagnostics;

/// <summary>
/// Mirrors user-facing toast notifications into the on-disk log via <see cref="LodestoneLog"/>, so the
/// diagnostic log captures everything the user was told — successes and, crucially, failures — without
/// every publisher having to log explicitly. This is the single chokepoint that makes the logs useful
/// for bug reports. Attach once at startup and dispose the returned token to detach.
/// </summary>
public static class DiagnosticLogger
{
    /// <summary>Subscribes to <see cref="ToastMessage"/> on the bus and records each one. Returns the
    /// subscription token; dispose it to stop mirroring.</summary>
    public static IDisposable Attach(IMessageBus bus)
    {
        ArgumentNullException.ThrowIfNull(bus);
        return bus.Subscribe<ToastMessage>(Record);
    }

    // Maps a toast's severity onto the matching log level so the on-disk level mirrors what the user saw.
    private static void Record(ToastMessage toast)
    {
        string line = Describe(toast);
        switch (toast.Kind)
        {
            case ToastKind.Error:
                LodestoneLog.Error(line);
                break;
            case ToastKind.Warning:
                LodestoneLog.Warn(line);
                break;
            default:
                LodestoneLog.Info(line);
                break;
        }
    }

    /// <summary>Formats a toast as one log line — "Title: Body", or just the title when the body is empty.
    /// Pure and side-effect-free so the formatting can be unit-tested without touching the log file.</summary>
    public static string Describe(ToastMessage toast)
    {
        ArgumentNullException.ThrowIfNull(toast);
        return string.IsNullOrWhiteSpace(toast.Body) ? toast.Title : $"{toast.Title}: {toast.Body}";
    }
}

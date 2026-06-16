using System.Globalization;

namespace Lodestone.Infrastructure.Persistence;

/// <summary>
/// Tiny append-only file logger to <c>%AppData%/Lodestone/logs</c>. Deliberately dependency-free and
/// best-effort (never throws) so it can be called from anywhere — including the global crash handler —
/// without becoming a failure source itself.
/// </summary>
public static class LodestoneLog
{
    private static readonly object Gate = new();

    public static void Info(string message) => Write("INFO", message);

    public static void Error(string message, Exception? exception = null)
        => Write("ERROR", exception is null ? message : $"{message}{Environment.NewLine}{exception}");

    private static void Write(string level, string text)
    {
        try
        {
            Directory.CreateDirectory(LodestonePaths.LogsDirectory);
            string file = Path.Combine(LodestonePaths.LogsDirectory, $"lodestone-{DateTime.UtcNow:yyyyMMdd}.log");
            string line = $"{DateTime.UtcNow.ToString("HH:mm:ss", CultureInfo.InvariantCulture)} [{level}] {text}{Environment.NewLine}";
            lock (Gate)
            {
                File.AppendAllText(file, line);
            }
        }
        catch (IOException)
        {
            // logging must never throw
        }
    }
}

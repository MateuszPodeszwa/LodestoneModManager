using System.IO;
using Lodestone.Infrastructure.Persistence;
using Velopack;

namespace Lodestone.App;

/// <summary>
/// Process entry point. <see cref="VelopackApp"/> must run first so install/update/uninstall hooks
/// are handled (and the process exits) before any WPF UI spins up; in a normal launch it returns
/// immediately and we hand off to the generated WPF <c>App.Main</c>.
/// </summary>
internal static class Program
{
    [STAThread]
    public static void Main()
    {
        VelopackApp.Build()
            // On uninstall, purge everything Lodestone persisted so a reinstall starts genuinely clean -
            // fresh onboarding, no stale settings/library, and the supporter token gone (a reinstall must
            // re-paste a fresh code).
            .OnBeforeUninstallFastCallback(_ => PurgeAppData())
            .Run();
        App.Main();
    }

    // Removes only Lodestone's own data: %AppData%\Lodestone (settings, library, entitlements, logs,
    // trash) and our %LocalAppData%\Lodestone\cache. It deliberately leaves the user's .minecraft (mods,
    // worlds, loaders) untouched - "Reset to clean" in Settings is the explicit way to undo those - and it
    // leaves Velopack's own program files for the uninstaller to remove.
    private static void PurgeAppData()
    {
        TryDeleteDirectory(LodestonePaths.Root);
        TryDeleteDirectory(LodestonePaths.CacheDirectory);
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: uninstall must never fail because of our data.
        }
    }
}

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
            // On uninstall, drop the supporter token so a reinstall requires re-pasting a fresh code.
            .OnBeforeUninstallFastCallback(_ => TryDeleteEntitlements())
            .Run();
        App.Main();
    }

    private static void TryDeleteEntitlements()
    {
        try
        {
            string path = LodestonePaths.EntitlementsFile;
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best effort: uninstall must never fail because of our data file.
        }
    }
}

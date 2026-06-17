using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using Lodestone.Infrastructure.Persistence;

namespace Lodestone.Infrastructure.Loaders;

/// <summary>
/// Installs Forge / NeoForge by downloading their official installer jar and launching it (their Java
/// GUI does the actual install). Versions are resolved from Forge's promotions feed and NeoForge's Maven
/// metadata; the installer runs under a Java runtime located from JAVA_HOME or the Minecraft launcher's
/// bundled runtimes. The resulting profile is then detected by <see cref="MinecraftGameInventory"/>.
/// </summary>
public sealed class ForgeInstallerLauncher : IExternalLoaderInstaller
{
    private const string ForgePromotions = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";
    private const string ForgeMaven = "https://maven.minecraftforge.net/net/minecraftforge/forge";
    private const string NeoForgeMeta = "https://maven.neoforged.net/releases/net/neoforged/neoforge/maven-metadata.xml";
    private const string NeoForgeMaven = "https://maven.neoforged.net/releases/net/neoforged/neoforge";

    private readonly HttpClient _http;

    public ForgeInstallerLauncher(HttpClient http) => _http = http;

    public bool Supports(Loader loader) => loader is Loader.Forge or Loader.NeoForge;

    public async Task<Result<string>> LaunchInstallerAsync(Loader loader, GameVersion version, CancellationToken ct = default)
    {
        Result<(string Url, string Version)> resolved = await ResolveInstallerAsync(loader, version, ct).ConfigureAwait(false);
        if (resolved.IsFailure)
        {
            return Result.Failure<string>(resolved.Error);
        }

        string? java = FindJava();
        if (java is null)
        {
            return Result.Failure<string>("loader.no_java",
                "Couldn't find a Java runtime to run the installer. Launch Minecraft once (it installs Java), or install Java, then try again.");
        }

        string jar;
        try
        {
            jar = await DownloadAsync(resolved.Value.Url, loader, resolved.Value.Version, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<string>("loader.network", ex.Message);
        }
        catch (IOException ex)
        {
            return Result.Failure<string>("loader.io", ex.Message);
        }

        try
        {
            Process.Start(new ProcessStartInfo(java, $"-jar \"{jar}\"")
            {
                UseShellExecute = false,
                WorkingDirectory = Path.GetDirectoryName(jar) ?? Environment.CurrentDirectory,
            });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            return Result.Failure<string>("loader.launch", "Couldn't start the installer: " + ex.Message);
        }

        return Result.Success(resolved.Value.Version);
    }

    /// <summary>Resolves the installer download URL and version (public for testing the version logic).</summary>
    public async Task<Result<(string Url, string Version)>> ResolveInstallerAsync(Loader loader, GameVersion version, CancellationToken ct = default)
    {
        try
        {
            return loader switch
            {
                Loader.Forge => await ResolveForgeAsync(version, ct).ConfigureAwait(false),
                Loader.NeoForge => await ResolveNeoForgeAsync(version, ct).ConfigureAwait(false),
                _ => Result.Failure<(string, string)>("loader.unsupported", $"{loader.ToDisplayName()} isn't an external-installer loader."),
            };
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure<(string, string)>("loader.network", ex.Message);
        }
        catch (Exception ex) when (ex is JsonException or System.Xml.XmlException)
        {
            return Result.Failure<(string, string)>("loader.parse", ex.Message);
        }
    }

    private async Task<Result<(string, string)>> ResolveForgeAsync(GameVersion version, CancellationToken ct)
    {
        string json = await _http.GetStringAsync(ForgePromotions, ct).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("promos", out JsonElement promos))
        {
            return Result.Failure<(string, string)>("loader.no_version", "Forge promotions were unavailable.");
        }

        // Prefer the recommended build, fall back to the latest.
        string? forgeVersion = TryProp(promos, $"{version.Value}-recommended") ?? TryProp(promos, $"{version.Value}-latest");
        if (forgeVersion is null)
        {
            return Result.Failure<(string, string)>("loader.no_version", $"No Forge build is published for Minecraft {version}.");
        }

        string combo = $"{version.Value}-{forgeVersion}";
        return ($"{ForgeMaven}/{combo}/forge-{combo}-installer.jar", forgeVersion);
    }

    private async Task<Result<(string, string)>> ResolveNeoForgeAsync(GameVersion version, CancellationToken ct)
    {
        string? prefix = NeoForgePrefix(version);
        if (prefix is null)
        {
            return Result.Failure<(string, string)>("loader.no_version", $"NeoForge doesn't target Minecraft {version}.");
        }

        string xml = await _http.GetStringAsync(NeoForgeMeta, ct).ConfigureAwait(false);
        string? best = null;
        foreach (XElement element in XDocument.Parse(xml).Descendants("version"))
        {
            string value = element.Value;
            if (value.StartsWith(prefix + ".", StringComparison.Ordinal) && (best is null || VersionComparer.IsNewer(value, best)))
            {
                best = value;
            }
        }

        if (best is null)
        {
            return Result.Failure<(string, string)>("loader.no_version", $"No NeoForge build is published for Minecraft {version}.");
        }

        return ($"{NeoForgeMaven}/{best}/neoforge-{best}-installer.jar", best);
    }

    // NeoForge versions drop Minecraft's leading "1.": 1.21 -> 21.0, 1.21.1 -> 21.1, 1.20.2 -> 20.2.
    private static string? NeoForgePrefix(GameVersion version)
    {
        string[] parts = version.Value.Split('.');
        if (parts.Length < 2 || parts[0] != "1")
        {
            return null;
        }

        string patch = parts.Length >= 3 ? parts[2] : "0";
        return $"{parts[1]}.{patch}";
    }

    private static string? TryProp(JsonElement obj, string name)
        => obj.TryGetProperty(name, out JsonElement el) && el.ValueKind == JsonValueKind.String ? el.GetString() : null;

    private async Task<string> DownloadAsync(string url, Loader loader, string version, CancellationToken ct)
    {
        string dir = Path.Combine(LodestonePaths.CacheDirectory, "installers");
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, $"{loader.ToSlug()}-{version}-installer.jar");
        if (File.Exists(file) && new FileInfo(file).Length > 0)
        {
            return file; // already cached
        }

        await using Stream source = await _http.GetStreamAsync(url, ct).ConfigureAwait(false);
        string temp = file + ".tmp";
        await using (FileStream destination = new(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await source.CopyToAsync(destination, ct).ConfigureAwait(false);
        }

        File.Move(temp, file, overwrite: true);
        return file;
    }

    // Locates a Java runtime: JAVA_HOME first, then the Minecraft launcher's bundled runtimes (the classic
    // launcher and the Microsoft Store package), preferring javaw.exe.
    private static string? FindJava()
    {
        string? javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            string candidate = Path.Combine(javaHome, "bin", "javaw.exe");
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        foreach (string root in RuntimeRoots())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            try
            {
                string? found = Directory.EnumerateFiles(root, "javaw.exe", SearchOption.AllDirectories).FirstOrDefault();
                if (found is not null)
                {
                    return found;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Skip an unreadable runtime root and try the next.
            }
        }

        return null;
    }

    private static IEnumerable<string> RuntimeRoots()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        yield return Path.Combine(programFilesX86, "Minecraft Launcher", "runtime");
        yield return Path.Combine(localAppData, "Packages", "Microsoft.4297127D64EC6_8wekyb3d8bbwe", "LocalCache", "Local", "runtime");
    }
}

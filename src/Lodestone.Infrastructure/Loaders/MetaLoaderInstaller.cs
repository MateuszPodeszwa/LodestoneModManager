using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Infrastructure.Loaders;

/// <summary>
/// Installs Fabric or Quilt by fetching a launcher profile from their meta APIs and writing it into
/// <c>versions/</c> plus a <c>launcher_profiles.json</c> entry — exactly what their official installers do,
/// but without a Java step (libraries are fetched by the launcher on first run). Forge/NeoForge are
/// reported unsupported because they need their own Java installers.
/// </summary>
public sealed class MetaLoaderInstaller : ILoaderInstaller
{
    private readonly HttpClient _http;
    private readonly ISettingsStore _settings;
    private readonly IGameLocator _locator;

    public MetaLoaderInstaller(HttpClient http, ISettingsStore settings, IGameLocator locator)
    {
        _http = http;
        _settings = settings;
        _locator = locator;
    }

    public bool Supports(Loader loader) => loader is Loader.Fabric or Loader.Quilt;

    public bool IsInstalled(Loader loader, GameVersion gameVersion)
    {
        string? game = _settings.Current.GameDirectory;
        if (string.IsNullOrWhiteSpace(game) || !Supports(loader))
        {
            return false;
        }

        string versions = Path.Combine(game, "versions");
        if (!Directory.Exists(versions))
        {
            return false;
        }

        string prefix = loader == Loader.Fabric ? "fabric-loader-" : "quilt-loader-";
        string suffix = "-" + gameVersion.Value;
        return Directory.EnumerateDirectories(versions).Any(d =>
        {
            string n = Path.GetFileName(d);
            return n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        });
    }

    public async Task<Result> EnsureInstalledAsync(Loader loader, GameVersion gameVersion, CancellationToken ct = default)
    {
        if (!Supports(loader))
        {
            return Result.Failure("loader.unsupported",
                $"{loader.ToDisplayName()} must be installed with its official installer; Lodestone installs Fabric and Quilt directly.");
        }

        string? game = _settings.Current.GameDirectory;
        if (string.IsNullOrWhiteSpace(game) || !_locator.IsValid(game))
        {
            return Result.Failure("game.dir_missing", "Set your Minecraft folder before installing a loader.");
        }

        if (IsInstalled(loader, gameVersion))
        {
            return Result.Success();
        }

        try
        {
            (string metaBase, string versionsPath) = loader == Loader.Fabric
                ? ("https://meta.fabricmc.net", "v2/versions/loader")
                : ("https://meta.quiltmc.org", "v3/versions/loader");

            string? loaderVersion = await ResolveLatestLoaderAsync(metaBase, versionsPath, gameVersion.Value, ct).ConfigureAwait(false);
            if (loaderVersion is null)
            {
                return Result.Failure("loader.no_version", $"No {loader.ToDisplayName()} build is available for {gameVersion}.");
            }

            string profileJson = await _http
                .GetStringAsync($"{metaBase}/{versionsPath}/{gameVersion.Value}/{loaderVersion}/profile/json", ct)
                .ConfigureAwait(false);

            using JsonDocument doc = JsonDocument.Parse(profileJson);
            if (!doc.RootElement.TryGetProperty("id", out JsonElement idEl) || idEl.GetString() is not { Length: > 0 } versionId)
            {
                return Result.Failure("loader.bad_profile", "The loader profile was missing an id.");
            }

            WriteVersionProfile(game, versionId, profileJson);
            UpdateLauncherProfiles(game, versionId, $"{loader.ToDisplayName()} {gameVersion.Value}");
            return Result.Success();
        }
        catch (HttpRequestException ex)
        {
            return Result.Failure("loader.network", ex.Message);
        }
        catch (JsonException ex)
        {
            return Result.Failure("loader.parse", ex.Message);
        }
        catch (IOException ex)
        {
            return Result.Failure("loader.io", ex.Message);
        }
    }

    private async Task<string?> ResolveLatestLoaderAsync(string metaBase, string versionsPath, string game, CancellationToken ct)
    {
        string json = await _http.GetStringAsync($"{metaBase}/{versionsPath}/{game}", ct).ConfigureAwait(false);
        using JsonDocument doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        // Prefer the first stable entry; the lists are newest-first.
        string? firstVersion = null;
        foreach (JsonElement entry in doc.RootElement.EnumerateArray())
        {
            if (!entry.TryGetProperty("loader", out JsonElement loaderEl) ||
                !loaderEl.TryGetProperty("version", out JsonElement verEl) ||
                verEl.GetString() is not { Length: > 0 } version)
            {
                continue;
            }

            firstVersion ??= version;
            bool stable = !loaderEl.TryGetProperty("stable", out JsonElement stableEl) || stableEl.ValueKind != JsonValueKind.False;
            if (stable)
            {
                return version;
            }
        }

        return firstVersion;
    }

    private static void WriteVersionProfile(string gameDir, string versionId, string profileJson)
    {
        string dir = Path.Combine(gameDir, "versions", versionId);
        Directory.CreateDirectory(dir);
        string target = Path.Combine(dir, versionId + ".json");
        string temp = target + ".tmp";
        File.WriteAllText(temp, profileJson);
        if (File.Exists(target))
        {
            File.Replace(temp, target, null);
        }
        else
        {
            File.Move(temp, target);
        }
    }

    private static void UpdateLauncherProfiles(string gameDir, string versionId, string name)
    {
        string path = Path.Combine(gameDir, "launcher_profiles.json");

        JsonObject root;
        if (File.Exists(path))
        {
            File.Copy(path, path + ".bak", overwrite: true); // safety net before editing the launcher's file
            root = JsonNode.Parse(File.ReadAllText(path)) as JsonObject ?? [];
        }
        else
        {
            root = [];
        }

        if (root["profiles"] is not JsonObject profiles)
        {
            profiles = [];
            root["profiles"] = profiles;
        }

        string now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        profiles[versionId] = new JsonObject
        {
            ["name"] = name,
            ["type"] = "custom",
            ["created"] = now,
            ["lastUsed"] = now,
            ["lastVersionId"] = versionId,
            ["icon"] = "Furnace",
        };

        root["version"] ??= 3;

        string temp = path + ".tmp";
        File.WriteAllText(temp, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
        if (File.Exists(path))
        {
            File.Replace(temp, path, null);
        }
        else
        {
            File.Move(temp, path);
        }
    }
}

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using Lodestone.Infrastructure.Persistence;

namespace Lodestone.Infrastructure.FileSystem;

/// <summary>
/// File-backed <see cref="ILauncherVisibility"/>. Hides inactive modded profiles by moving their
/// <c>launcher_profiles.json</c> entries (matched by <c>lastVersionId</c>) into a stash file, verbatim,
/// and restores them on demand — so the user's custom JVM args, game dirs and icons survive a switch.
/// <c>launcher_profiles.json</c> is backed up to <c>.bak</c> before every edit; vanilla and unrelated
/// profiles are left untouched.
/// </summary>
public sealed class LauncherProfileStore : ILauncherVisibility
{
    private readonly ISettingsStore _settings;
    private readonly string? _stashPathOverride;

    public LauncherProfileStore(ISettingsStore settings, string? stashPath = null)
    {
        _settings = settings;
        _stashPathOverride = stashPath;
    }

    private string StashPath => _stashPathOverride ?? LodestonePaths.LauncherStashFile;

    public Result Apply(LoaderProfile? active, IReadOnlyList<LoaderProfile> allModded)
    {
        string? game = _settings.Current.GameDirectory;
        if (string.IsNullOrWhiteSpace(game))
        {
            return Result.Success();
        }

        string path = Path.Combine(game, "launcher_profiles.json");
        if (!File.Exists(path))
        {
            return Result.Success(); // no launcher file yet — nothing to hide
        }

        try
        {
            var moddedIds = new HashSet<string>(allModded.Select(p => p.VersionId), StringComparer.OrdinalIgnoreCase);
            string? activeId = active?.VersionId;

            JsonObject root = ReadObject(path) ?? [];
            if (root["profiles"] is not JsonObject profiles)
            {
                profiles = [];
                root["profiles"] = profiles;
            }

            JsonObject stash = ReadObject(StashPath) ?? [];
            File.Copy(path, path + ".bak", overwrite: true);

            // Stash every modded entry that isn't the active one (preserved verbatim, matched by lastVersionId).
            foreach (string key in profiles.Select(p => p.Key).ToList())
            {
                string? lastVid = LastVersionId(profiles[key]);
                if (lastVid is not null && moddedIds.Contains(lastVid) &&
                    !string.Equals(lastVid, activeId, StringComparison.OrdinalIgnoreCase))
                {
                    stash[key] = Clone(profiles[key]!);
                    profiles.Remove(key);
                }
            }

            if (activeId is not null)
            {
                EnsureActive(profiles, stash, active!, activeId);
            }

            root["version"] ??= 3;
            WriteObject(path, root);
            WriteObject(StashPath, stash);
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return Result.Failure("launcher.io", ex.Message);
        }
    }

    public Result RestoreAll()
    {
        string? game = _settings.Current.GameDirectory;
        if (string.IsNullOrWhiteSpace(game))
        {
            return Result.Success();
        }

        try
        {
            JsonObject stash = ReadObject(StashPath) ?? [];
            if (stash.Count == 0)
            {
                return Result.Success();
            }

            string path = Path.Combine(game, "launcher_profiles.json");
            JsonObject root = (File.Exists(path) ? ReadObject(path) : null) ?? [];
            if (root["profiles"] is not JsonObject profiles)
            {
                profiles = [];
                root["profiles"] = profiles;
            }

            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
            }

            foreach (KeyValuePair<string, JsonNode?> entry in stash.ToList())
            {
                if (!profiles.ContainsKey(entry.Key))
                {
                    profiles[entry.Key] = Clone(entry.Value!);
                }
            }

            root["version"] ??= 3;
            WriteObject(path, root);
            File.Delete(StashPath); // stash is now empty
            return Result.Success();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return Result.Failure("launcher.io", ex.Message);
        }
    }

    // Surfaces the active profile: restore its stashed entry if we hid it before, else create one; then
    // mark it as the most-recently-used so the launcher selects it.
    private static void EnsureActive(JsonObject profiles, JsonObject stash, LoaderProfile active, string activeId)
    {
        bool live = profiles.Any(e => string.Equals(LastVersionId(e.Value), activeId, StringComparison.OrdinalIgnoreCase));
        if (!live)
        {
            string? stashKey = stash
                .FirstOrDefault(e => string.Equals(LastVersionId(e.Value), activeId, StringComparison.OrdinalIgnoreCase)).Key;
            if (stashKey is not null)
            {
                profiles[stashKey] = Clone(stash[stashKey]!);
                stash.Remove(stashKey);
            }
            else
            {
                profiles[activeId] = NewEntry(active);
            }
        }

        string now = Timestamp();
        foreach (KeyValuePair<string, JsonNode?> entry in profiles)
        {
            if (entry.Value is JsonObject obj && string.Equals(LastVersionId(entry.Value), activeId, StringComparison.OrdinalIgnoreCase))
            {
                obj["lastUsed"] = now;
            }
        }
    }

    private static JsonObject NewEntry(LoaderProfile profile)
    {
        string now = Timestamp();
        return new JsonObject
        {
            ["name"] = profile.Label,
            ["type"] = "custom",
            ["created"] = now,
            ["lastUsed"] = now,
            ["lastVersionId"] = profile.VersionId,
            ["icon"] = "Furnace",
        };
    }

    // Safe read of a profile entry's lastVersionId (null when missing or not a string).
    private static string? LastVersionId(JsonNode? entry)
        => entry is JsonObject obj && obj["lastVersionId"] is JsonValue value && value.TryGetValue(out string? id) ? id : null;

    private static JsonObject? ReadObject(string path)
        => File.Exists(path) && JsonNode.Parse(File.ReadAllText(path)) is JsonObject obj ? obj : null;

    private static JsonNode Clone(JsonNode node) => JsonNode.Parse(node.ToJsonString())!;

    private static string Timestamp() => DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private static void WriteObject(string path, JsonObject obj)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temp = path + ".tmp";
        File.WriteAllText(temp, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
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

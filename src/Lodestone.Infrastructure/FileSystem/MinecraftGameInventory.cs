using System.Text.Json;
using System.Text.RegularExpressions;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;

namespace Lodestone.Infrastructure.FileSystem;

/// <summary>
/// Reads the base game versions installed under <c>&lt;game&gt;/versions/</c>. Each entry has a
/// <c>&lt;id&gt;.json</c> launcher manifest: a vanilla version's <c>id</c> is the version itself
/// (e.g. <c>1.21.4</c>); a modded profile carries <c>inheritsFrom</c> naming its base version. We map
/// every profile to its base, dedupe and sort newest-first. When a manifest is missing or unreadable
/// we fall back to a trailing-version token in the folder name (e.g. <c>fabric-loader-0.16.5-1.21.4</c>).
/// </summary>
public sealed partial class MinecraftGameInventory : IGameInventory
{
    private static readonly Regex LoaderFolder = BuildLoaderFolderPattern();
    private static readonly Regex Snapshot = BuildSnapshotPattern();

    private readonly ISettingsStore _settings;

    public MinecraftGameInventory(ISettingsStore settings) => _settings = settings;

    public bool IsVersionInstalled(GameVersion version) => InstalledVersions().Any(v => v.Equals(version));

    public IReadOnlyList<GameVersion> InstalledVersions()
    {
        string? game = _settings.Current.GameDirectory;
        if (string.IsNullOrWhiteSpace(game))
        {
            return [];
        }

        string versions = Path.Combine(game, "versions");
        if (!Directory.Exists(versions))
        {
            return [];
        }

        var byValue = new Dictionary<string, GameVersion>(StringComparer.OrdinalIgnoreCase);
        foreach (string directory in Directory.EnumerateDirectories(versions))
        {
            string folder = Path.GetFileName(directory);
            GameVersion? baseVersion = ResolveBaseVersion(directory, folder);
            if (baseVersion is not null && !byValue.ContainsKey(baseVersion.Value))
            {
                byValue[baseVersion.Value] = baseVersion;
            }
        }

        return byValue.Values.OrderByDescending(v => v).ToList();
    }

    // Prefer the manifest (inheritsFrom for modded, id for vanilla); fall back to the folder name.
    private static GameVersion? ResolveBaseVersion(string directory, string folder)
    {
        string manifest = Path.Combine(directory, folder + ".json");
        if (File.Exists(manifest))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(manifest));
                JsonElement root = doc.RootElement;
                GameVersion? fromManifest = PlausibleVersion(TryString(root, "inheritsFrom"))
                                            ?? PlausibleVersion(TryString(root, "id"));
                if (fromManifest is not null)
                {
                    return fromManifest;
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                // Unreadable/corrupt manifest — fall through to the folder-name heuristic.
            }
        }

        // No usable manifest: Fabric/Quilt fold the base version into the folder name as a suffix;
        // anything else is a vanilla folder named for the version itself.
        Match loader = LoaderFolder.Match(folder);
        return loader.Success
            ? PlausibleVersion(loader.Groups["mc"].Value)
            : PlausibleVersion(folder);
    }

    private static string? TryString(JsonElement root, string property)
        => root.ValueKind == JsonValueKind.Object &&
           root.TryGetProperty(property, out JsonElement el) &&
           el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;

    // GameVersion.Create accepts any non-blank string as a snapshot, so guard against treating a loader
    // folder name (e.g. "fabric-loader-0.16.5-1.21.4") as a version: accept only releases and snapshots.
    private static GameVersion? PlausibleVersion(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        GameVersion? version = GameVersion.Create(raw).Match<GameVersion?>(v => v, _ => null);
        return version is not null && (version.IsRelease || Snapshot.IsMatch(version.Value)) ? version : null;
    }

    [GeneratedRegex(@"^(?:fabric|quilt)-loader-.+-(?<mc>\d+(?:\.\d+){1,2}|\d{2}w\d{2}[a-z])$", RegexOptions.CultureInvariant)]
    private static partial Regex BuildLoaderFolderPattern();

    [GeneratedRegex(@"^\d{2}w\d{2}[a-z]$", RegexOptions.CultureInvariant)]
    private static partial Regex BuildSnapshotPattern();
}

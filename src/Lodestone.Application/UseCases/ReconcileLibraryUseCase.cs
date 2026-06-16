using System.IO;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Common;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>
/// Auto-discovery: scans the game's mods/resourcepacks/shaderpacks folders and imports any files that
/// aren't already in the library (e.g. mods the user dropped in manually, or an existing install Lodestone
/// is seeing for the first time). Purely additive — it never deletes records, so it's safe to run on every
/// start/refresh.
/// </summary>
public sealed class ReconcileLibraryUseCase
{
    private static readonly ContentType[] AllTypes = [ContentType.Mod, ContentType.ResourcePack, ContentType.Shader];
    private const string DisabledSuffix = ".disabled";

    private readonly IInstalledContentRepository _repository;
    private readonly IContentInstaller _installer;
    private readonly IArchiveMetadataReader _reader;
    private readonly ISettingsStore _settings;
    private readonly IGameLocator _locator;

    public ReconcileLibraryUseCase(
        IInstalledContentRepository repository,
        IContentInstaller installer,
        IArchiveMetadataReader reader,
        ISettingsStore settings,
        IGameLocator locator)
    {
        _repository = repository;
        _installer = installer;
        _reader = reader;
        _settings = settings;
        _locator = locator;
    }

    public async Task<Result<int>> ExecuteAsync(GameVersion? targetVersion, CancellationToken ct = default)
    {
        if (!_locator.IsValid(_settings.Current.GameDirectory))
        {
            return Result.Success(0);
        }

        IReadOnlyList<InstalledContent> all = await _repository.GetAllAsync(ct).ConfigureAwait(false);
        var trackedBaseNames = all
            .Where(i => !string.IsNullOrWhiteSpace(i.FileName))
            .Select(i => BaseName(i.FileName!))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int imported = 0;

        foreach (ContentType type in AllTypes)
        {
            foreach (string path in _installer.EnumerateInstalledFiles(type))
            {
                ct.ThrowIfCancellationRequested();

                string fileName = Path.GetFileName(path);
                string baseName = BaseName(fileName);
                if (!trackedBaseNames.Add(baseName))
                {
                    continue; // already tracked (or seen this pass)
                }

                Result<LocalContentMetadata> metaResult = await _reader.ReadAsync(path, ct).ConfigureAwait(false);
                LocalContentMetadata? meta = metaResult.IsSuccess ? metaResult.Value : null;

                bool enabled = !fileName.EndsWith(DisabledSuffix, StringComparison.OrdinalIgnoreCase);
                string name = !string.IsNullOrWhiteSpace(meta?.Name)
                    ? meta!.Name!
                    : Slug.PrettifyFileName(Path.GetFileNameWithoutExtension(baseName));

                Loader loader = Loader.None;
                if (type.UsesLoader())
                {
                    loader = meta is { LoadersOrEmpty.Count: > 0 } ? meta.LoadersOrEmpty[0] : _settings.Current.DefaultLoader;
                }

                List<GameVersion> versions = meta is { GameVersionsOrEmpty.Count: > 0 }
                    ? meta.GameVersionsOrEmpty.ToList()
                    : targetVersion is not null ? [targetVersion] : [];

                string id = !string.IsNullOrWhiteSpace(meta?.ModId) ? meta!.ModId! : Slug.From(name);
                if (await _repository.FindAsync(id, ct).ConfigureAwait(false) is not null)
                {
                    id = $"{id}-{Slug.From(baseName)}";
                    if (await _repository.FindAsync(id, ct).ConfigureAwait(false) is not null)
                    {
                        continue; // can't form a unique id; leave it
                    }
                }

                IReadOnlyList<string> provided =
                    meta is { ProvidedIdsOrEmpty.Count: > 0 } ? meta.ProvidedIdsOrEmpty
                    : !string.IsNullOrWhiteSpace(meta?.ModId) ? [meta!.ModId!]
                    : [];

                var content = new InstalledContent(id, name, type)
                {
                    Author = "Local file",
                    Version = string.IsNullOrWhiteSpace(meta?.Version) ? "unknown" : meta!.Version!,
                    Loader = loader,
                    GameVersions = versions,
                    Enabled = enabled,
                    Source = "local",
                    FileName = fileName,
                    SizeMb = SafeLength(path) / (1024.0 * 1024.0),
                    Dependencies = meta?.DependenciesOrEmpty ?? [],
                    ProvidedIds = provided,
                };

                await _repository.UpsertAsync(content, ct).ConfigureAwait(false);
                imported++;
            }
        }

        return Result.Success(imported);
    }

    private static string BaseName(string fileName)
        => fileName.EndsWith(DisabledSuffix, StringComparison.OrdinalIgnoreCase)
            ? fileName[..^DisabledSuffix.Length]
            : fileName;

    private static long SafeLength(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (IOException)
        {
            return 0;
        }
    }
}

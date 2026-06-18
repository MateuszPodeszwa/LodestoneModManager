using Lodestone.Application.Abstractions;
using Lodestone.Application.Common;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>
/// The drag-and-drop pipeline: inspect a local archive, place it in the correct folder, and record
/// it in the library tagged with the currently selected game version (plus any versions the file
/// itself declares). Auto-detects the content type from the archive contents.
/// </summary>
public sealed class InstallLocalFileUseCase
{
    private readonly IArchiveMetadataReader _reader;
    private readonly IContentInstaller _installer;
    private readonly IInstalledContentRepository _repository;
    private readonly ISettingsStore _settings;
    private readonly IGameInventory _inventory;

    public InstallLocalFileUseCase(
        IArchiveMetadataReader reader,
        IContentInstaller installer,
        IInstalledContentRepository repository,
        ISettingsStore settings,
        IGameInventory inventory)
    {
        _reader = reader;
        _installer = installer;
        _repository = repository;
        _settings = settings;
        _inventory = inventory;
    }

    public async Task<Result<InstalledContent>> ExecuteAsync(
        string filePath,
        GameVersion? targetVersion,
        CancellationToken ct = default)
    {
        Result<LocalContentMetadata> metaResult = await _reader.ReadAsync(filePath, ct).ConfigureAwait(false);
        if (metaResult.IsFailure)
        {
            return Result.Failure<InstalledContent>(metaResult.Error);
        }

        LocalContentMetadata meta = metaResult.Value;

        // Mods need their loader installed for the target version, or the dropped file won't load. Gate the
        // active loader (resource packs and shaders don't use a loader; with no target version we can't tell).
        if (meta.Type.UsesLoader() && targetVersion is not null)
        {
            Loader active = _settings.Current.DefaultLoader;
            if (active == Loader.None)
            {
                return Result.Failure<InstalledContent>("install.no_loader",
                    "Choose a mod loader in Settings before installing mods.");
            }

            if (!_inventory.IsLoaderInstalled(active, targetVersion))
            {
                return Result.Failure<InstalledContent>("install.loader_missing",
                    $"Install the {active.ToDisplayName()} loader for {targetVersion} first — open Settings → Loader version.");
            }
        }

        Result<PlaceResult> placed = await _installer
            .PlaceAsync(filePath, meta.Type, DuplicateResolution.KeepBoth, ct)
            .ConfigureAwait(false);
        if (placed.IsFailure)
        {
            return Result.Failure<InstalledContent>(placed.Error);
        }

        string fileStem = Path.GetFileNameWithoutExtension(filePath);
        string name = !string.IsNullOrWhiteSpace(meta.Name) ? meta.Name! : Slug.PrettifyFileName(fileStem);

        Loader loader = meta.Type.UsesLoader()
            ? meta.LoadersOrEmpty.Count > 0 ? meta.LoadersOrEmpty[0] : _settings.Current.DefaultLoader
            : Loader.None;

        // The item supports whatever it declares, plus the profile version the user dropped it onto
        // (when there is one - with no game version installed yet, we keep only what the file declares).
        var versions = meta.GameVersionsOrEmpty.ToList();
        if (targetVersion is not null && !versions.Any(v => v.Equals(targetVersion)))
        {
            versions.Add(targetVersion);
        }

        IReadOnlyList<string> provided =
            meta.ProvidedIdsOrEmpty.Count > 0 ? meta.ProvidedIdsOrEmpty
            : !string.IsNullOrWhiteSpace(meta.ModId) ? [meta.ModId!]
            : [];

        string id = !string.IsNullOrWhiteSpace(meta.ModId) ? meta.ModId! : Slug.From(name);

        var content = new InstalledContent(id, name, meta.Type)
        {
            Author = "Local file",
            Version = string.IsNullOrWhiteSpace(meta.Version) ? "1.0.0" : meta.Version!,
            Loader = loader,
            GameVersions = versions,
            Enabled = true,
            Source = "local",
            FileName = placed.Value.FileName,
            SizeMb = placed.Value.SizeBytes / (1024.0 * 1024.0),
            Dependencies = meta.DependenciesOrEmpty,
            ProvidedIds = provided,
        };

        await _repository.UpsertAsync(content, ct).ConfigureAwait(false);
        return content;
    }
}

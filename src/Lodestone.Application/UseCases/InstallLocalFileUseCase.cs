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

    public InstallLocalFileUseCase(
        IArchiveMetadataReader reader,
        IContentInstaller installer,
        IInstalledContentRepository repository,
        ISettingsStore settings)
    {
        _reader = reader;
        _installer = installer;
        _repository = repository;
        _settings = settings;
    }

    public async Task<Result<InstalledContent>> ExecuteAsync(
        string filePath,
        GameVersion targetVersion,
        CancellationToken ct = default)
    {
        Result<LocalContentMetadata> metaResult = await _reader.ReadAsync(filePath, ct).ConfigureAwait(false);
        if (metaResult.IsFailure)
        {
            return Result.Failure<InstalledContent>(metaResult.Error);
        }

        LocalContentMetadata meta = metaResult.Value;

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

        // The item supports whatever it declares, plus the profile version the user dropped it onto.
        var versions = meta.GameVersionsOrEmpty.ToList();
        if (!versions.Any(v => v.Equals(targetVersion)))
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

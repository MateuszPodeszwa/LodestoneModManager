using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>
/// One-click install from the Browse screen: resolve the best compatible build, download &amp; verify
/// it, place it, and record it. Refuses to install a build that doesn't match the active game version
/// and loader (the resolver returns nothing in that case).
/// </summary>
public sealed class InstallFromCatalogUseCase
{
    private readonly IModSourceRegistry _registry;
    private readonly IVersionResolver _resolver;
    private readonly IDownloader _downloader;
    private readonly IContentInstaller _installer;
    private readonly IInstalledContentRepository _repository;
    private readonly ISettingsStore _settings;

    public InstallFromCatalogUseCase(
        IModSourceRegistry registry,
        IVersionResolver resolver,
        IDownloader downloader,
        IContentInstaller installer,
        IInstalledContentRepository repository,
        ISettingsStore settings)
    {
        _registry = registry;
        _resolver = resolver;
        _downloader = downloader;
        _installer = installer;
        _repository = repository;
        _settings = settings;
    }

    public async Task<Result<InstalledContent>> ExecuteAsync(
        CatalogProject project,
        GameVersion targetVersion,
        Loader loader,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (await _repository.FindAsync(project.Id, ct).ConfigureAwait(false) is not null)
        {
            return Result.Failure<InstalledContent>("install.duplicate", $"{project.Name} is already installed.");
        }

        IModSource source = _registry.Find(project.Source) ?? _registry.Primary;

        Result<IReadOnlyList<ProjectVersion>> versions =
            await source.GetVersionsAsync(project.Id, ct).ConfigureAwait(false);
        if (versions.IsFailure)
        {
            return Result.Failure<InstalledContent>(versions.Error);
        }

        Loader effectiveLoader = project.Type.UsesLoader() ? loader : Loader.None;
        ProjectVersion? chosen = _resolver.Resolve(versions.Value, targetVersion, effectiveLoader);
        if (chosen is null)
        {
            return Result.Failure<InstalledContent>(
                "install.no_compatible_version",
                $"No build of {project.Name} supports {targetVersion}" +
                (project.Type.UsesLoader() ? $" on {loader.ToDisplayName()}." : "."));
        }

        Result<DownloadedFile> download = await _downloader
            .DownloadAsync(new DownloadRequest(chosen.DownloadUrl, chosen.FileName, chosen.Sha512), progress, ct)
            .ConfigureAwait(false);
        if (download.IsFailure)
        {
            return Result.Failure<InstalledContent>(download.Error);
        }

        Result<PlaceResult> placed = await _installer
            .PlaceAsync(download.Value.Path, project.Type, DuplicateResolution.Replace, ct)
            .ConfigureAwait(false);
        if (placed.IsFailure)
        {
            return Result.Failure<InstalledContent>(placed.Error);
        }

        Loader contentLoader = Loader.None;
        if (project.Type.UsesLoader())
        {
            contentLoader = chosen.SupportsLoader(loader) && loader != Loader.None
                ? loader
                : chosen.Loaders.Count > 0 ? chosen.Loaders[0] : _settings.Current.DefaultLoader;
        }

        var content = new InstalledContent(project.Id, project.Name, project.Type)
        {
            Author = project.Author,
            Version = chosen.VersionNumber,
            Loader = contentLoader,
            GameVersions = chosen.GameVersions,
            Enabled = true,
            ProjectId = project.Id,
            Source = project.Source,
            FileName = placed.Value.FileName,
            Sha512 = download.Value.Sha512,
            SizeMb = download.Value.SizeBytes / (1024.0 * 1024.0),
            Dependencies = chosen.Dependencies,
            ProvidedIds = [project.Slug],
            IsLibrary = project.Categories.Any(c => string.Equals(c, "library", StringComparison.OrdinalIgnoreCase)),
        };

        await _repository.UpsertAsync(content, ct).ConfigureAwait(false);
        return content;
    }
}

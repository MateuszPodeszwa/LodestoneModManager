using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>The result of a catalog install: the requested item plus the names of any required
/// dependencies that were pulled in automatically (empty when the user already had them all).</summary>
public sealed record CatalogInstall(InstalledContent Item, IReadOnlyList<string> InstalledDependencies);

/// <summary>
/// One-click install from the Browse screen: resolve the best compatible build, download &amp; verify
/// it, place it, and record it — then do the same for any <see cref="DependencyKind.Required"/>
/// dependencies the build declares (transitively), so the user isn't left with a mod that can't load.
/// Refuses to install a build that doesn't match the active game version and loader (the resolver
/// returns nothing in that case); a dependency that can't be resolved is left for the compatibility
/// report rather than failing the whole install.
/// </summary>
public sealed class InstallFromCatalogUseCase
{
    private readonly IModSourceRegistry _registry;
    private readonly IVersionResolver _resolver;
    private readonly IDownloader _downloader;
    private readonly IContentInstaller _installer;
    private readonly IInstalledContentRepository _repository;
    private readonly ISettingsStore _settings;
    private readonly IGameInventory _inventory;

    public InstallFromCatalogUseCase(
        IModSourceRegistry registry,
        IVersionResolver resolver,
        IDownloader downloader,
        IContentInstaller installer,
        IInstalledContentRepository repository,
        ISettingsStore settings,
        IGameInventory inventory)
    {
        _registry = registry;
        _resolver = resolver;
        _downloader = downloader;
        _installer = installer;
        _repository = repository;
        _settings = settings;
        _inventory = inventory;
    }

    public async Task<Result<CatalogInstall>> ExecuteAsync(
        CatalogProject project,
        GameVersion targetVersion,
        Loader loader,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (await _repository.FindAsync(project.Id, ct).ConfigureAwait(false) is not null)
        {
            return Result.Failure<CatalogInstall>("install.duplicate", $"{project.Name} is already installed.");
        }

        // A mod can't load without its loader actually installed for the target version, so block the
        // install rather than leave the user with a mod that silently does nothing. Resource packs and
        // shaders don't use a loader, so they're never gated this way.
        if (project.Type.UsesLoader())
        {
            if (loader == Loader.None)
            {
                return Result.Failure<CatalogInstall>("install.no_loader",
                    "Choose a mod loader in Settings before installing mods.");
            }

            if (!_inventory.IsLoaderInstalled(loader, targetVersion))
            {
                return Result.Failure<CatalogInstall>("install.loader_missing",
                    $"Install the {loader.ToDisplayName()} loader for {targetVersion} first — open Settings → Loader version.");
            }
        }

        IModSource source = _registry.Find(project.Source) ?? _registry.Primary;

        Result<InstalledContent> primary = await InstallOneAsync(source, project, targetVersion, loader, progress, ct).ConfigureAwait(false);
        if (primary.IsFailure)
        {
            return Result.Failure<CatalogInstall>(primary.Error);
        }

        // Pull in required dependencies (e.g. Fabric API) so the mod can actually load.
        var installedDependencies = new List<string>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { project.Id };
        await InstallRequiredDependenciesAsync(
            source, primary.Value.Dependencies, targetVersion, loader, installedDependencies, visited, ct).ConfigureAwait(false);

        return new CatalogInstall(primary.Value, installedDependencies);
    }

    /// <summary>Breadth-first install of every still-missing required dependency, transitively.</summary>
    private async Task InstallRequiredDependenciesAsync(
        IModSource source,
        IReadOnlyList<Dependency> dependencies,
        GameVersion targetVersion,
        Loader loader,
        List<string> installed,
        HashSet<string> visited,
        CancellationToken ct)
    {
        var queue = new Queue<Dependency>(dependencies.Where(IsRequired));

        while (queue.Count > 0)
        {
            ct.ThrowIfCancellationRequested();

            string identifier = queue.Dequeue().Identifier;
            if (!visited.Add(identifier))
            {
                continue; // already handled this project in this run (also breaks dependency cycles)
            }

            if (await _repository.FindAsync(identifier, ct).ConfigureAwait(false) is not null)
            {
                continue; // the user already has it
            }

            Result<CatalogProject> project = await source.GetProjectAsync(identifier, ct).ConfigureAwait(false);
            if (project.IsFailure)
            {
                continue; // can't resolve metadata — the compatibility engine will flag it as missing
            }

            Result<InstalledContent> installedDependency =
                await InstallOneAsync(source, project.Value, targetVersion, loader, null, ct).ConfigureAwait(false);
            if (installedDependency.IsFailure)
            {
                continue; // e.g. no build for this version yet — leave it for the compatibility report
            }

            installed.Add(project.Value.Name);

            foreach (Dependency next in installedDependency.Value.Dependencies.Where(IsRequired))
            {
                queue.Enqueue(next);
            }
        }

        static bool IsRequired(Dependency d) => d.Kind == DependencyKind.Required && !string.IsNullOrWhiteSpace(d.Identifier);
    }

    /// <summary>Resolves, downloads, verifies, places and records a single project. No duplicate check —
    /// callers decide whether the project should be (re)installed.</summary>
    private async Task<Result<InstalledContent>> InstallOneAsync(
        IModSource source,
        CatalogProject project,
        GameVersion targetVersion,
        Loader loader,
        IProgress<TransferProgress>? progress,
        CancellationToken ct)
    {
        Result<IReadOnlyList<ProjectVersion>> versions = await source.GetVersionsAsync(project.Id, ct).ConfigureAwait(false);
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
            IconUrl = project.IconUrl,
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

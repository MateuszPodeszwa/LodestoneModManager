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
/// it, place it, and record it - then do the same for any <see cref="DependencyKind.Required"/>
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
        // "Already installed" is profile-scoped: the same mod can live in several profiles at once (its own
        // build per profile), so it's a duplicate only when an existing build serves *this* (loader + version)
        // profile. A build for a different version or loader is a separate profile - it's installed alongside
        // rather than blocked or overwritten. Packs/shaders are loader-agnostic, so any present copy counts.
        IReadOnlyList<InstalledContent> existing = await _repository.GetAllAsync(ct).ConfigureAwait(false);
        if (existing.Any(i => IsSameProject(i, project.Id) && i.ServesProfile(loader, targetVersion)))
        {
            return Result.Failure<CatalogInstall>("install.duplicate", AlreadyInstalledMessage(project, targetVersion, loader));
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

        // Modrinth version metadata only carries each dependency's project id, so the Dependency
        // records on the primary mod (and on each installed dependency) have no human DisplayName.
        // Capture id -> human name for every dependency we resolve a CatalogProject for, then backfill
        // the recorded items so the compatibility badge reads "Requires Fabric API", not the raw id.
        var resolvedNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var installedItems = new List<InstalledContent> { primary.Value };
        await InstallRequiredDependenciesAsync(
            source, primary.Value.Dependencies, targetVersion, loader,
            installedDependencies, visited, resolvedNames, installedItems, ct).ConfigureAwait(false);

        await BackfillDependencyNamesAsync(installedItems, resolvedNames, ct).ConfigureAwait(false);

        return new CatalogInstall(primary.Value, installedDependencies);
    }

    /// <summary>Breadth-first install of every still-missing required dependency, transitively.</summary>
    /// <param name="resolvedNames">Accumulates <c>identifier -&gt; human project name</c> for every
    /// dependency a project was resolved for - including ones already installed or that failed to
    /// install - so the caller can backfill missing dependency display names.</param>
    /// <param name="installedItems">Accumulates each item newly written to the repository in this run,
    /// so the caller can re-persist them once dependency names are known.</param>
    private async Task InstallRequiredDependenciesAsync(
        IModSource source,
        IReadOnlyList<Dependency> dependencies,
        GameVersion targetVersion,
        Loader loader,
        List<string> installed,
        HashSet<string> visited,
        Dictionary<string, string> resolvedNames,
        List<InstalledContent> installedItems,
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

            Result<CatalogProject> project = await source.GetProjectAsync(identifier, ct).ConfigureAwait(false);
            if (project.IsFailure)
            {
                continue; // can't resolve metadata - the compatibility engine will flag it as missing
            }

            // Record the human name as soon as we have it, before any skip below, so even
            // already-installed or unbuildable dependencies still resolve to a readable label.
            resolvedNames[identifier] = project.Value.Name;

            IReadOnlyList<InstalledContent> have = await _repository.GetAllAsync(ct).ConfigureAwait(false);
            if (have.Any(i => i.Provides(identifier) && i.ServesProfile(loader, targetVersion)))
            {
                continue; // the user already has it for this profile (a build for another profile doesn't count)
            }

            Result<InstalledContent> installedDependency =
                await InstallOneAsync(source, project.Value, targetVersion, loader, null, ct).ConfigureAwait(false);
            if (installedDependency.IsFailure)
            {
                continue; // e.g. no build for this version yet - leave it for the compatibility report
            }

            installed.Add(project.Value.Name);
            installedItems.Add(installedDependency.Value);

            foreach (Dependency next in installedDependency.Value.Dependencies.Where(IsRequired))
            {
                queue.Enqueue(next);
            }
        }

        static bool IsRequired(Dependency d) => d.Kind == DependencyKind.Required && !string.IsNullOrWhiteSpace(d.Identifier);
    }

    /// <summary>
    /// Rewrites each installed item's <see cref="Dependency"/> records to set a human
    /// <see cref="Dependency.DisplayName"/> from <paramref name="resolvedNames"/> wherever it's still
    /// null, then re-persists the items whose dependencies changed. Modrinth only gives us project ids
    /// at install time, so this second pass is what turns "Requires 9s6osm5g" into "Requires Cloth
    /// Config" in My Content. Items with nothing to fill in are left untouched (no redundant writes).
    /// </summary>
    private async Task BackfillDependencyNamesAsync(
        IReadOnlyList<InstalledContent> items,
        Dictionary<string, string> resolvedNames,
        CancellationToken ct)
    {
        if (resolvedNames.Count == 0)
        {
            return;
        }

        foreach (InstalledContent item in items)
        {
            var rewritten = new List<Dependency>(item.Dependencies.Count);
            bool changed = false;

            foreach (Dependency dep in item.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dep.DisplayName) &&
                    resolvedNames.TryGetValue(dep.Identifier, out string? name))
                {
                    rewritten.Add(dep with { DisplayName = name });
                    changed = true;
                }
                else
                {
                    rewritten.Add(dep);
                }
            }

            if (changed)
            {
                item.Dependencies = rewritten;
                await _repository.UpsertAsync(item, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>Resolves, downloads, verifies, places and records a single project. No duplicate check -
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

        string recordId = await ResolveRecordIdAsync(project.Id, contentLoader, targetVersion, ct).ConfigureAwait(false);
        var content = new InstalledContent(recordId, project.Name, project.Type)
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
            Categories = project.Categories.Select(c => c.ToLowerInvariant()).Distinct().ToList(),
        };

        await _repository.UpsertAsync(content, ct).ConfigureAwait(false);
        return content;
    }

    // The record id for a freshly installed build. The first build of a project keeps the bare project id
    // (so existing libraries are unchanged); any later build - the same mod for another profile - gets a
    // profile-scoped id so it's a distinct record with its own file instead of overwriting the first. The
    // caller has already ruled out a build that serves this exact profile, so we never clobber a live one.
    private async Task<string> ResolveRecordIdAsync(string projectId, Loader loader, GameVersion version, CancellationToken ct)
        => await _repository.FindAsync(projectId, ct).ConfigureAwait(false) is null
            ? projectId
            : $"{projectId}@{loader.ToSlug()}-{version.Value}";

    // A library record represents the given catalog project when either its id or its project id matches -
    // coexisting builds share the project id while taking distinct record ids.
    private static bool IsSameProject(InstalledContent item, string projectId)
        => string.Equals(item.ProjectId, projectId, StringComparison.OrdinalIgnoreCase)
           || string.Equals(item.Id, projectId, StringComparison.OrdinalIgnoreCase);

    private static string AlreadyInstalledMessage(CatalogProject project, GameVersion version, Loader loader)
        => project.Type.UsesLoader()
            ? $"{project.Name} is already installed for {version} · {loader.ToDisplayName()}."
            : $"{project.Name} is already installed.";
}

using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>
/// Backfills human <see cref="Dependency.DisplayName"/>s onto already-installed content. Modrinth's
/// version metadata only carries each dependency's project id, so a mod added before install-time name
/// capture existed (or one imported from disk) renders "Requires 9s6osm5g" in the compatibility report
/// instead of "Requires Cloth Config". This pass resolves the real project name for each dependency id
/// still missing one - via the declaring item's mod source (a <c>GetProjectAsync</c> the caching source
/// memoises) - and persists it, so the badge becomes readable.
///
/// Best-effort: ids that can't be resolved (a loader mod-id that isn't a source slug, or a network error)
/// are left untouched. The network is only hit for ids whose name isn't already known, so once a library
/// has been backfilled subsequent runs resolve nothing and make no calls.
/// </summary>
public sealed class ResolveDependencyNamesUseCase
{
    private readonly IModSourceRegistry _registry;
    private readonly IInstalledContentRepository _repository;

    public ResolveDependencyNamesUseCase(IModSourceRegistry registry, IInstalledContentRepository repository)
    {
        _registry = registry;
        _repository = repository;
    }

    /// <summary>
    /// Resolves and persists any missing dependency names. Returns the number of installed items whose
    /// records were updated (0 when nothing needed resolving), so the caller can decide whether to refresh
    /// the view.
    /// </summary>
    public async Task<int> ExecuteAsync(CancellationToken ct = default)
    {
        IReadOnlyList<InstalledContent> items = await _repository.GetAllAsync(ct).ConfigureAwait(false);

        // Group the dependency ids still missing a name by the source that can resolve them (the declaring
        // item's source - a dependency id is a project id/slug within that same source).
        var pending = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (InstalledContent item in items)
        {
            string source = string.IsNullOrWhiteSpace(item.Source) ? _registry.Primary.Name : item.Source;
            foreach (Dependency dep in item.Dependencies)
            {
                if (!NeedsName(dep))
                {
                    continue;
                }

                if (!pending.TryGetValue(source, out HashSet<string>? ids))
                {
                    ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    pending[source] = ids;
                }

                ids.Add(dep.Identifier);
            }
        }

        if (pending.Count == 0)
        {
            return 0;
        }

        var names = await ResolveNamesAsync(pending, ct).ConfigureAwait(false);
        if (names.Count == 0)
        {
            return 0;
        }

        int updated = 0;
        foreach (InstalledContent item in items)
        {
            if (TryApplyNames(item, names))
            {
                await _repository.UpsertAsync(item, ct).ConfigureAwait(false);
                updated++;
            }
        }

        return updated;
    }

    /// <summary>Resolves <c>id -&gt; human name</c> for each pending id via its source; unresolved ids are omitted.</summary>
    private async Task<Dictionary<string, string>> ResolveNamesAsync(
        Dictionary<string, HashSet<string>> pending, CancellationToken ct)
    {
        var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach ((string sourceName, HashSet<string> ids) in pending)
        {
            IModSource? source = _registry.Find(sourceName);
            if (source is null || !source.IsConfigured)
            {
                continue;
            }

            foreach (string id in ids)
            {
                ct.ThrowIfCancellationRequested();
                Result<CatalogProject> project = await source.GetProjectAsync(id, ct).ConfigureAwait(false);
                if (project.IsSuccess && !string.IsNullOrWhiteSpace(project.Value.Name))
                {
                    names[id] = project.Value.Name;
                }
            }
        }

        return names;
    }

    /// <summary>Rewrites <paramref name="item"/>'s dependencies with any newly-known names; returns true if it changed.</summary>
    private static bool TryApplyNames(InstalledContent item, Dictionary<string, string> names)
    {
        var rewritten = new List<Dependency>(item.Dependencies.Count);
        bool changed = false;
        foreach (Dependency dep in item.Dependencies)
        {
            if (NeedsName(dep) && names.TryGetValue(dep.Identifier, out string? name))
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
        }

        return changed;
    }

    private static bool NeedsName(Dependency dep)
        => !string.IsNullOrWhiteSpace(dep.Identifier) && string.IsNullOrWhiteSpace(dep.DisplayName);
}

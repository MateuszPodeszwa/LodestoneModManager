using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>
/// Updates every item currently flagged with an available update to its latest compatible build
/// (the "Update all" action). Unlike <see cref="RefreshUpdatesUseCase"/> this always applies, and is
/// invoked explicitly by the user regardless of the auto-update setting.
/// </summary>
public sealed class UpdateAllUseCase
{
    private readonly IInstalledContentRepository _repository;
    private readonly IModSourceRegistry _registry;
    private readonly IVersionResolver _resolver;
    private readonly IUpdateContentUseCase _updateContent;

    public UpdateAllUseCase(
        IInstalledContentRepository repository,
        IModSourceRegistry registry,
        IVersionResolver resolver,
        IUpdateContentUseCase updateContent)
    {
        _repository = repository;
        _registry = registry;
        _resolver = resolver;
        _updateContent = updateContent;
    }

    public async Task<Result<int>> ExecuteAsync(GameVersion? activeVersion, CancellationToken ct = default)
    {
        IReadOnlyList<InstalledContent> items = await _repository.GetAllAsync(ct).ConfigureAwait(false);
        int updated = 0;

        foreach (InstalledContent item in items.Where(i => i.UpdateAvailable))
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(item.ProjectId) || _registry.Find(item.Source) is not { IsConfigured: true } source)
            {
                continue;
            }

            Result<IReadOnlyList<ProjectVersion>> versions = await source.GetVersionsAsync(item.ProjectId!, ct).ConfigureAwait(false);
            if (versions.IsFailure)
            {
                continue;
            }

            GameVersion? checkVersion = activeVersion ?? item.GameVersions.OrderByDescending(v => v).FirstOrDefault();
            if (checkVersion is null)
            {
                continue;
            }

            ProjectVersion? latest = _resolver.Resolve(versions.Value, checkVersion, item.Loader);
            if (latest is not null && (await _updateContent.ApplyAsync(item, latest, null, ct).ConfigureAwait(false)).IsSuccess)
            {
                updated++;
            }
        }

        return Result.Success(updated);
    }
}

using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>The tally from an update refresh: how many updates are now available (flagged) and how
/// many were auto-applied in place.</summary>
public sealed record UpdateSummary(int UpdatesAvailable, int Updated);

/// <summary>
/// Checks every catalog-sourced item for a newer compatible build. Runs only when invoked — on app
/// start and on manual refresh — never on a timer. When auto-update is on, enabled items are updated
/// in place; otherwise they are flagged so the UI can badge them.
/// </summary>
public sealed class RefreshUpdatesUseCase
{
    private readonly IInstalledContentRepository _repository;
    private readonly IModSourceRegistry _registry;
    private readonly IVersionResolver _resolver;
    private readonly IUpdateContentUseCase _updateContent;
    private readonly ISettingsStore _settings;

    public RefreshUpdatesUseCase(
        IInstalledContentRepository repository,
        IModSourceRegistry registry,
        IVersionResolver resolver,
        IUpdateContentUseCase updateContent,
        ISettingsStore settings)
    {
        _repository = repository;
        _registry = registry;
        _resolver = resolver;
        _updateContent = updateContent;
        _settings = settings;
    }

    public async Task<Result<UpdateSummary>> ExecuteAsync(GameVersion? activeVersion, CancellationToken ct = default)
    {
        IReadOnlyList<InstalledContent> items = await _repository.GetAllAsync(ct).ConfigureAwait(false);
        int available = 0;
        int updated = 0;

        foreach (InstalledContent item in items)
        {
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(item.ProjectId) || item.Source is not ("modrinth" or "curseforge"))
            {
                continue;
            }

            IModSource? source = _registry.Find(item.Source);
            if (source is null || !source.IsConfigured)
            {
                continue;
            }

            // Backfill a missing catalog icon (installs before icons were captured predate this field) so
            // My Content shows real artwork instead of the letter avatar. One-time per item: once the icon
            // is stored this block is skipped on subsequent refreshes.
            if (string.IsNullOrWhiteSpace(item.IconUrl))
            {
                Result<CatalogProject> meta = await source.GetProjectAsync(item.ProjectId!, ct).ConfigureAwait(false);
                if (meta.IsSuccess && !string.IsNullOrWhiteSpace(meta.Value.IconUrl))
                {
                    item.IconUrl = meta.Value.IconUrl;
                    await _repository.UpsertAsync(item, ct).ConfigureAwait(false);
                }
            }

            Result<IReadOnlyList<ProjectVersion>> versions =
                await source.GetVersionsAsync(item.ProjectId!, ct).ConfigureAwait(false);
            if (versions.IsFailure)
            {
                continue; // one source hiccup must not fail the whole refresh
            }

            // Check each build against a version it actually supports: the active one when this build serves
            // it, otherwise the build's own newest version. The same mod can be installed for several
            // profiles (issue #44), so a build set aside for another version mustn't be checked against — and
            // updated to — the active profile's version.
            GameVersion? checkVersion = activeVersion is not null && item.SupportsVersion(activeVersion)
                ? activeVersion
                : item.GameVersions.OrderByDescending(v => v).FirstOrDefault() ?? activeVersion;
            if (checkVersion is null)
            {
                continue;
            }

            ProjectVersion? latest = _resolver.Resolve(versions.Value, checkVersion, item.Loader);
            if (latest is null)
            {
                continue;
            }

            bool isNewer = VersionComparer.IsNewer(latest.VersionNumber, item.Version);
            if (isNewer)
            {
                available++;

                if (_settings.Current.AutoUpdate && item.Enabled)
                {
                    Result applied = await _updateContent.ApplyAsync(item, latest, null, ct).ConfigureAwait(false);
                    if (applied.IsSuccess)
                    {
                        updated++;
                        available--; // it's been handled, so no longer "available"
                        continue;
                    }
                }

                item.UpdateAvailable = true;
                await _repository.UpsertAsync(item, ct).ConfigureAwait(false);
            }
            else if (item.UpdateAvailable)
            {
                item.UpdateAvailable = false;
                await _repository.UpsertAsync(item, ct).ConfigureAwait(false);
            }
        }

        return new UpdateSummary(available, updated);
    }
}

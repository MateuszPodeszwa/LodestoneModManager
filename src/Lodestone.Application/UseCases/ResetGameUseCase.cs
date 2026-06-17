using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>How much a reset removed: tracked content items and managed loader profiles.</summary>
public sealed record ResetSummary(int ContentRemoved, int LoadersRemoved);

/// <summary>
/// Resets the Minecraft install to a pre-Lodestone, pre-loader state: soft-deletes every piece of
/// content Lodestone tracks (mods/packs/shaders go to the recoverable trash), removes the loader profiles
/// Lodestone installed (tracked in the ledger — Fabric/Quilt and Forge/NeoForge), and clears the
/// active-profile selection. The user's vanilla versions, their worlds, and any loader they installed
/// outside Lodestone are left untouched.
/// </summary>
public sealed class ResetGameUseCase
{
    private readonly IInstalledContentRepository _repository;
    private readonly IContentInstaller _installer;
    private readonly ILoaderInstaller _loaders;
    private readonly ISettingsStore _settings;
    private readonly ILauncherVisibility _launcher;

    public ResetGameUseCase(
        IInstalledContentRepository repository,
        IContentInstaller installer,
        ILoaderInstaller loaders,
        ISettingsStore settings,
        ILauncherVisibility launcher)
    {
        _repository = repository;
        _installer = installer;
        _loaders = loaders;
        _settings = settings;
        _launcher = launcher;
    }

    public async Task<Result<ResetSummary>> ExecuteAsync(CancellationToken ct = default)
    {
        IReadOnlyList<InstalledContent> all = await _repository.GetAllAsync(ct).ConfigureAwait(false);

        int content = 0;
        foreach (InstalledContent item in all)
        {
            if (!string.IsNullOrWhiteSpace(item.FileName))
            {
                Result removed = await _installer.RemoveAsync(item.Type, item.FileName!, ct).ConfigureAwait(false);
                if (removed.IsFailure)
                {
                    return Result.Failure<ResetSummary>(removed.Error); // e.g. a file is locked (Minecraft running)
                }
            }

            await _repository.RemoveAsync(item.Id, ct).ConfigureAwait(false);
            content++;
        }

        // Un-hide any profiles parked in the stash so the launcher reflects reality before we strip loaders.
        _launcher.RestoreAll();

        Result<int> loaders = await _loaders.RemoveManagedAsync(ct).ConfigureAwait(false);
        if (loaders.IsFailure)
        {
            return Result.Failure<ResetSummary>(loaders.Error);
        }

        // Drop the active-profile selection so the UI returns to the neutral "All profiles" state.
        LodestoneSettings reset = _settings.Current.Clone();
        reset.SelectedVersion = LodestoneSettings.DefaultSelectedVersion;
        reset.SelectedLoader = Loader.None;
        await _settings.SaveAsync(reset, ct).ConfigureAwait(false);

        return new ResetSummary(content, loaders.Value);
    }
}

using Lodestone.Application.Abstractions;
using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>Replaces an installed item's file with a newer build (extracted for testability/DIP).</summary>
public interface IUpdateContentUseCase
{
    Task<Result> ApplyAsync(
        InstalledContent item,
        ProjectVersion target,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Replaces an installed item's file with a newer build. Downloads &amp; verifies first, soft-deletes
/// the old file, then places the new one — so a failed download leaves the working install intact.
/// </summary>
public sealed class UpdateContentUseCase : IUpdateContentUseCase
{
    private readonly IDownloader _downloader;
    private readonly IContentInstaller _installer;
    private readonly IInstalledContentRepository _repository;

    public UpdateContentUseCase(
        IDownloader downloader,
        IContentInstaller installer,
        IInstalledContentRepository repository)
    {
        _downloader = downloader;
        _installer = installer;
        _repository = repository;
    }

    public async Task<Result> ApplyAsync(
        InstalledContent item,
        ProjectVersion target,
        IProgress<TransferProgress>? progress = null,
        CancellationToken ct = default)
    {
        Result<DownloadedFile> download = await _downloader
            .DownloadAsync(new DownloadRequest(target.DownloadUrl, target.FileName, target.Sha512), progress, ct)
            .ConfigureAwait(false);
        if (download.IsFailure)
        {
            return Result.Failure(download.Error);
        }

        // Best-effort removal of the previous file (it goes to trash, so it's recoverable).
        if (!string.IsNullOrWhiteSpace(item.FileName))
        {
            await _installer.RemoveAsync(item.Type, item.FileName!, ct).ConfigureAwait(false);
        }

        Result<PlaceResult> placed = await _installer
            .PlaceAsync(download.Value.Path, item.Type, DuplicateResolution.Replace, ct)
            .ConfigureAwait(false);
        if (placed.IsFailure)
        {
            return Result.Failure(placed.Error);
        }

        item.Version = target.VersionNumber;
        item.FileName = placed.Value.FileName;
        item.Sha512 = download.Value.Sha512;
        item.GameVersions = target.GameVersions;
        item.Dependencies = target.Dependencies;
        item.SizeMb = download.Value.SizeBytes / (1024.0 * 1024.0);
        item.UpdateAvailable = false;

        await _repository.UpsertAsync(item, ct).ConfigureAwait(false);
        return Result.Success();
    }
}

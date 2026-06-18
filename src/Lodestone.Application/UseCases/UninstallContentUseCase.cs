using Lodestone.Application.Abstractions;
using Lodestone.Domain.Common;

namespace Lodestone.Application.UseCases;

/// <summary>Removes an item: soft-deletes its file (to trash) then drops it from the library.</summary>
public sealed class UninstallContentUseCase
{
    private readonly IInstalledContentRepository _repository;
    private readonly IContentInstaller _installer;

    public UninstallContentUseCase(IInstalledContentRepository repository, IContentInstaller installer)
    {
        _repository = repository;
        _installer = installer;
    }

    public async Task<Result> ExecuteAsync(string id, CancellationToken ct = default)
    {
        Domain.InstalledContent? item = await _repository.FindAsync(id, ct).ConfigureAwait(false);
        if (item is null)
        {
            return Result.Success(); // already gone - idempotent
        }

        if (!string.IsNullOrWhiteSpace(item.FileName))
        {
            Result removed = await _installer.RemoveAsync(item.Type, item.FileName!, ct).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed; // e.g. file locked because Minecraft is running
            }
        }

        await _repository.RemoveAsync(id, ct).ConfigureAwait(false);
        return Result.Success();
    }
}

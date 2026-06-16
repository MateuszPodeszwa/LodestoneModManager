using Lodestone.Domain;

namespace Lodestone.Application.Abstractions;

/// <summary>
/// Persistence of the user's library (Repository pattern). Implementations are responsible for
/// durably storing each mutation; callers treat it as the source of truth for installed content.
/// </summary>
public interface IInstalledContentRepository
{
    Task<IReadOnlyList<InstalledContent>> GetAllAsync(CancellationToken ct = default);

    Task<InstalledContent?> FindAsync(string id, CancellationToken ct = default);

    /// <summary>Insert or update an item, then persist.</summary>
    Task UpsertAsync(InstalledContent content, CancellationToken ct = default);

    /// <summary>Remove an item by id, then persist. No-op if absent.</summary>
    Task RemoveAsync(string id, CancellationToken ct = default);
}

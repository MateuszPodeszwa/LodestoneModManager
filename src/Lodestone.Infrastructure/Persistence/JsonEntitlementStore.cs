using Lodestone.Application.Supporter;

namespace Lodestone.Infrastructure.Persistence;

/// <summary>
/// File-backed store for the redeemed supporter token. Persists the signed code itself (not a trusted
/// flag); <see cref="SupporterService"/> re-verifies its signature on load, so a hand-edited file can't
/// fabricate supporter status.
/// </summary>
public sealed class JsonEntitlementStore : IEntitlementStore
{
    private readonly string _path;

    public JsonEntitlementStore(string? path = null) => _path = path ?? LodestonePaths.EntitlementsFile;

    public StoredEntitlement? Current { get; private set; }

    public event EventHandler? Changed;

    public async Task<StoredEntitlement?> LoadAsync(CancellationToken ct = default)
    {
        Current = await JsonStore.ReadAsync<StoredEntitlement>(_path, ct).ConfigureAwait(false);
        Changed?.Invoke(this, EventArgs.Empty); // let the service derive status once state is loaded
        return Current;
    }

    public async Task SaveAsync(StoredEntitlement entitlement, CancellationToken ct = default)
    {
        await JsonStore.WriteAsync(_path, entitlement, ct).ConfigureAwait(false);
        Current = entitlement;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        try
        {
            if (File.Exists(_path))
            {
                File.Delete(_path);
            }
        }
        catch (IOException)
        {
            // Non-fatal: clearing in-memory state below is what matters.
        }

        Current = null;
        Changed?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }
}

using Lodestone.Application.Abstractions;

namespace Lodestone.Infrastructure.Persistence;

/// <summary>
/// File-backed <see cref="ILoaderLedger"/>: a small JSON list of the loader profiles Lodestone installed,
/// stored alongside its other data. Writes are atomic (see <see cref="JsonStore"/>) and the list is tiny
/// (one entry per installed loader profile), so each call reads the current file rather than caching -
/// keeping it correct even if another process edited it. Installs are serialised by the app's operation
/// gate, so there is no concurrent writer to contend with.
/// </summary>
public sealed class JsonLoaderLedger : ILoaderLedger
{
    private readonly string _path;

    public JsonLoaderLedger(string? path = null) => _path = path ?? LodestonePaths.LoaderLedgerFile;

    public async Task<IReadOnlyList<LoaderInstall>> AllAsync(CancellationToken ct = default)
        => await ReadAsync(ct).ConfigureAwait(false);

    public async Task RecordAsync(LoaderInstall install, CancellationToken ct = default)
    {
        List<LoaderInstall> items = await ReadAsync(ct).ConfigureAwait(false);
        items.RemoveAll(i => string.Equals(i.VersionId, install.VersionId, StringComparison.OrdinalIgnoreCase));
        items.Add(install);
        await JsonStore.WriteAsync(_path, items, ct).ConfigureAwait(false);
    }

    public async Task ForgetAsync(IReadOnlyCollection<string> versionIds, CancellationToken ct = default)
    {
        if (versionIds.Count == 0)
        {
            return;
        }

        var drop = versionIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<LoaderInstall> items = await ReadAsync(ct).ConfigureAwait(false);
        if (items.RemoveAll(i => drop.Contains(i.VersionId)) > 0)
        {
            await JsonStore.WriteAsync(_path, items, ct).ConfigureAwait(false);
        }
    }

    private async Task<List<LoaderInstall>> ReadAsync(CancellationToken ct)
        => await JsonStore.ReadAsync<List<LoaderInstall>>(_path, ct).ConfigureAwait(false) ?? [];
}

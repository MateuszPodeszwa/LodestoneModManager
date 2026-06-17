using Lodestone.Application.Abstractions;

namespace Lodestone.Infrastructure.Tests;

/// <summary>In-memory <see cref="ILoaderLedger"/> for tests that don't exercise persistence.</summary>
internal sealed class FakeLoaderLedger : ILoaderLedger
{
    public List<LoaderInstall> Items { get; } = [];

    public Task<IReadOnlyList<LoaderInstall>> AllAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<LoaderInstall>>(Items.ToList());

    public Task RecordAsync(LoaderInstall install, CancellationToken ct = default)
    {
        Items.RemoveAll(i => string.Equals(i.VersionId, install.VersionId, StringComparison.OrdinalIgnoreCase));
        Items.Add(install);
        return Task.CompletedTask;
    }

    public Task ForgetAsync(IReadOnlyCollection<string> versionIds, CancellationToken ct = default)
    {
        var drop = versionIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Items.RemoveAll(i => drop.Contains(i.VersionId));
        return Task.CompletedTask;
    }
}

using Lodestone.Application.Abstractions;
using Lodestone.Domain;
using Lodestone.Infrastructure.Persistence;

namespace Lodestone.Infrastructure.Tests;

public class JsonLoaderLedgerTests
{
    private static JsonLoaderLedger Build(TempDir dir) => new(dir.File("loaders.json"));

    [Fact]
    public async Task Empty_when_no_file_exists()
    {
        using var dir = new TempDir();
        (await Build(dir).AllAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Records_and_reads_back_across_instances()
    {
        using var dir = new TempDir();
        await Build(dir).RecordAsync(new LoaderInstall("fabric-loader-0.16.5-1.21.4", Loader.Fabric, "1.21.4", "0.16.5", DateTimeOffset.UtcNow));

        IReadOnlyList<LoaderInstall> all = await Build(dir).AllAsync(); // a fresh instance reads the same file
        all.ShouldHaveSingleItem();
        all[0].VersionId.ShouldBe("fabric-loader-0.16.5-1.21.4");
        all[0].Loader.ShouldBe(Loader.Fabric);
        all[0].GameVersion.ShouldBe("1.21.4");
        all[0].LoaderVersion.ShouldBe("0.16.5");
    }

    [Fact]
    public async Task Record_replaces_an_entry_with_the_same_version_id()
    {
        using var dir = new TempDir();
        JsonLoaderLedger ledger = Build(dir);
        await ledger.RecordAsync(new LoaderInstall("neoforge-21.1.9", Loader.NeoForge, "1.21.1", "21.1.9", DateTimeOffset.UtcNow));
        await ledger.RecordAsync(new LoaderInstall("neoforge-21.1.9", Loader.NeoForge, "1.21.1", "21.1.9", DateTimeOffset.UtcNow));

        (await ledger.AllAsync()).ShouldHaveSingleItem(); // not duplicated
    }

    [Fact]
    public async Task Forget_removes_only_the_named_ids()
    {
        using var dir = new TempDir();
        JsonLoaderLedger ledger = Build(dir);
        await ledger.RecordAsync(new LoaderInstall("a", Loader.Fabric, "1.21.4", "1", DateTimeOffset.UtcNow));
        await ledger.RecordAsync(new LoaderInstall("b", Loader.Quilt, "1.21.4", "2", DateTimeOffset.UtcNow));

        await ledger.ForgetAsync(["a"]);

        IReadOnlyList<LoaderInstall> all = await ledger.AllAsync();
        all.ShouldHaveSingleItem();
        all[0].VersionId.ShouldBe("b");
    }
}

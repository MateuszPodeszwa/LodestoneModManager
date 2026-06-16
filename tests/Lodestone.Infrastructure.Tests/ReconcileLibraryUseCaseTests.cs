using System.IO;
using Lodestone.Application.Settings;
using Lodestone.Application.UseCases;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using Lodestone.Infrastructure.Archives;
using Lodestone.Infrastructure.FileSystem;
using Lodestone.Infrastructure.Persistence;

namespace Lodestone.Infrastructure.Tests;

public class ReconcileLibraryUseCaseTests
{
    [Fact]
    public async Task Imports_untracked_mods_found_on_disk_and_skips_already_tracked()
    {
        using var dir = new TempDir();
        string gameDir = dir.File("game");
        Directory.CreateDirectory(Path.Combine(gameDir, "mods"));
        ZipFixtures.Create(Path.Combine(gameDir, "mods", "sodium.jar"),
            ("fabric.mod.json", """{ "id": "sodium", "name": "Sodium", "version": "0.5.8" }"""));

        var settings = new JsonSettingsStore(dir.File("settings.json"));
        await settings.SaveAsync(new LodestoneSettings { GameDirectory = gameDir });

        using var repo = new JsonInstalledContentRepository(dir.File("library.json"));
        var useCase = new ReconcileLibraryUseCase(
            repo,
            new FileSystemContentInstaller(settings, dir.File("trash")),
            new ArchiveMetadataReader(),
            settings,
            new MinecraftGameLocator());

        Result<int> first = await useCase.ExecuteAsync(GameVersion.Parse("1.21.4"));
        first.Value.ShouldBe(1);

        IReadOnlyList<InstalledContent> items = await repo.GetAllAsync();
        items.ShouldContain(i => i.Name == "Sodium" && i.Loader == Loader.Fabric && i.Source == "local");

        // Running again finds nothing new.
        (await useCase.ExecuteAsync(GameVersion.Parse("1.21.4"))).Value.ShouldBe(0);
    }

    [Fact]
    public async Task Does_nothing_when_the_game_directory_is_invalid()
    {
        using var dir = new TempDir();
        var settings = new JsonSettingsStore(dir.File("settings.json")); // no game dir
        using var repo = new JsonInstalledContentRepository(dir.File("library.json"));
        var useCase = new ReconcileLibraryUseCase(
            repo,
            new FileSystemContentInstaller(settings, dir.File("trash")),
            new ArchiveMetadataReader(),
            settings,
            new MinecraftGameLocator());

        (await useCase.ExecuteAsync(GameVersion.Parse("1.21.4"))).Value.ShouldBe(0);
    }
}

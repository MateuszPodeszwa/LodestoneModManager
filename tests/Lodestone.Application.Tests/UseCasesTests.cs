using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Application.Settings;
using Lodestone.Application.UseCases;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using NSubstitute;

namespace Lodestone.Application.Tests;

public class InstallLocalFileUseCaseTests
{
    [Fact]
    public async Task Installs_local_jar_tagging_metadata_and_target_version()
    {
        var reader = Substitute.For<IArchiveMetadataReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(
            Result.Success(new LocalContentMetadata(
                ContentType.Mod,
                ModId: "sodium",
                Name: "Sodium",
                Version: "0.5.8",
                Loaders: [Loader.Fabric],
                Dependencies: [new Dependency("fabric-api", DependencyKind.Required)],
                ProvidedIds: ["sodium"],
                GameVersions: [GameVersion.Parse("1.21.4")])));

        var installer = Substitute.For<IContentInstaller>();
        installer.PlaceAsync(Arg.Any<string>(), ContentType.Mod, Arg.Any<DuplicateResolution>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PlaceResult("sodium-0.5.8.jar", 1_200_000, false)));

        var repo = Substitute.For<IInstalledContentRepository>();
        var settings = Substitute.For<ISettingsStore>();
        settings.Current.Returns(new LodestoneSettings { DefaultLoader = Loader.Fabric });

        var useCase = new InstallLocalFileUseCase(reader, installer, repo, settings);

        Result<InstalledContent> result = await useCase.ExecuteAsync(@"C:\drop\sodium.jar", GameVersion.Parse("1.20.1"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Sodium");
        result.Value.Loader.ShouldBe(Loader.Fabric);
        result.Value.Source.ShouldBe("local");
        result.Value.GameVersions.ShouldContain(v => v.Value == "1.21.4");
        result.Value.GameVersions.ShouldContain(v => v.Value == "1.20.1"); // the dropped-onto profile
        result.Value.Dependencies.ShouldContain(d => d.Identifier == "fabric-api");
        await repo.Received(1).UpsertAsync(Arg.Any<InstalledContent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Propagates_a_reader_failure_without_touching_the_library()
    {
        var reader = Substitute.For<IArchiveMetadataReader>();
        reader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<LocalContentMetadata>("archive.corrupt", "Not a valid archive."));
        var installer = Substitute.For<IContentInstaller>();
        var repo = Substitute.For<IInstalledContentRepository>();
        var settings = Substitute.For<ISettingsStore>();
        settings.Current.Returns(new LodestoneSettings());

        var useCase = new InstallLocalFileUseCase(reader, installer, repo, settings);

        Result<InstalledContent> result = await useCase.ExecuteAsync(@"C:\drop\bad.jar", GameVersion.Parse("1.21.4"));

        result.IsFailure.ShouldBeTrue();
        await repo.DidNotReceive().UpsertAsync(Arg.Any<InstalledContent>(), Arg.Any<CancellationToken>());
    }
}

public class InstallFromCatalogUseCaseTests
{
    private static CatalogProject Sodium() => new(
        "sodium", "sodium", "Sodium", "CaffeineMC", ContentType.Mod, "Fast renderer",
        12_400_000, 41_000, ["optimization"], [Loader.Fabric], [GameVersion.Parse("1.21.4")], "modrinth");

    private static ProjectVersion SodiumBuild(Loader loader = Loader.Fabric) => new(
        "v1", "sodium", "0.5.8", ContentType.Mod,
        [GameVersion.Parse("1.21.4")], [loader], [], "sodium-0.5.8.jar", "https://cdn/sodium", "deadbeef", 1.2);

    private static (InstallFromCatalogUseCase UseCase, IInstalledContentRepository Repo) Build(
        IReadOnlyList<ProjectVersion> versions,
        InstalledContent? existing = null)
    {
        var source = Substitute.For<IModSource>();
        source.IsConfigured.Returns(true);
        source.GetVersionsAsync("sodium", Arg.Any<CancellationToken>())
            .Returns(Result.Success(versions));

        var registry = Substitute.For<IModSourceRegistry>();
        registry.Find("modrinth").Returns(source);
        registry.Primary.Returns(source);

        var downloader = Substitute.For<IDownloader>();
        downloader.DownloadAsync(Arg.Any<DownloadRequest>(), Arg.Any<IProgress<TransferProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new DownloadedFile(@"C:\tmp\sodium.jar", 1_200_000, "deadbeef")));

        var installer = Substitute.For<IContentInstaller>();
        installer.PlaceAsync(Arg.Any<string>(), ContentType.Mod, Arg.Any<DuplicateResolution>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PlaceResult("sodium-0.5.8.jar", 1_200_000, false)));

        var repo = Substitute.For<IInstalledContentRepository>();
        repo.FindAsync("sodium", Arg.Any<CancellationToken>()).Returns(existing);

        var settings = Substitute.For<ISettingsStore>();
        settings.Current.Returns(new LodestoneSettings());

        return (new InstallFromCatalogUseCase(registry, new VersionResolver(), downloader, installer, repo, settings), repo);
    }

    [Fact]
    public async Task Resolves_downloads_and_records_the_install()
    {
        (InstallFromCatalogUseCase useCase, IInstalledContentRepository repo) = Build([SodiumBuild()]);

        Result<CatalogInstall> result =
            await useCase.ExecuteAsync(Sodium(), GameVersion.Parse("1.21.4"), Loader.Fabric);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Item.Version.ShouldBe("0.5.8");
        result.Value.Item.ProjectId.ShouldBe("sodium");
        result.Value.Item.Sha512.ShouldBe("deadbeef");
        result.Value.InstalledDependencies.ShouldBeEmpty();
        await repo.Received(1).UpsertAsync(Arg.Any<InstalledContent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refuses_to_install_what_is_already_installed()
    {
        (InstallFromCatalogUseCase useCase, _) = Build([SodiumBuild()], existing: Make.Mod("sodium"));

        Result<CatalogInstall> result =
            await useCase.ExecuteAsync(Sodium(), GameVersion.Parse("1.21.4"), Loader.Fabric);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("install.duplicate");
    }

    [Fact]
    public async Task Fails_when_no_build_matches_the_active_loader()
    {
        (InstallFromCatalogUseCase useCase, _) = Build([SodiumBuild(Loader.Forge)]);

        Result<CatalogInstall> result =
            await useCase.ExecuteAsync(Sodium(), GameVersion.Parse("1.21.4"), Loader.Fabric);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("install.no_compatible_version");
    }

    [Fact]
    public async Task Auto_installs_required_dependencies_from_the_source()
    {
        var source = Substitute.For<IModSource>();
        source.IsConfigured.Returns(true);

        // Sodium declares a required dependency on Fabric API (a Modrinth project id).
        IReadOnlyList<ProjectVersion> sodiumVersions =
        [
            new ProjectVersion(
                "v1", "sodium", "0.5.8", ContentType.Mod,
                [GameVersion.Parse("1.21.4")], [Loader.Fabric],
                [new Dependency("fabric-api", DependencyKind.Required)],
                "sodium-0.5.8.jar", "https://cdn/sodium", "deadbeef", 1.2),
        ];
        source.GetVersionsAsync("sodium", Arg.Any<CancellationToken>()).Returns(Result.Success(sodiumVersions));

        var fabricApi = new CatalogProject(
            "fabric-api", "fabric-api", "Fabric API", "FabricMC", ContentType.Mod, "Hooks",
            5_000_000, 9_000, ["library"], [Loader.Fabric], [GameVersion.Parse("1.21.4")], "modrinth");
        source.GetProjectAsync("fabric-api", Arg.Any<CancellationToken>()).Returns(Result.Success(fabricApi));

        IReadOnlyList<ProjectVersion> fabricApiVersions =
        [
            new ProjectVersion(
                "fv1", "fabric-api", "0.100.0", ContentType.Mod,
                [GameVersion.Parse("1.21.4")], [Loader.Fabric], [],
                "fabric-api-0.100.0.jar", "https://cdn/fapi", "cafebabe", 2.0),
        ];
        source.GetVersionsAsync("fabric-api", Arg.Any<CancellationToken>()).Returns(Result.Success(fabricApiVersions));

        var registry = Substitute.For<IModSourceRegistry>();
        registry.Find("modrinth").Returns(source);
        registry.Primary.Returns(source);

        var downloader = Substitute.For<IDownloader>();
        downloader.DownloadAsync(Arg.Any<DownloadRequest>(), Arg.Any<IProgress<TransferProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new DownloadedFile(@"C:\tmp\dep.jar", 1_000_000, "filehash")));

        var installer = Substitute.For<IContentInstaller>();
        installer.PlaceAsync(Arg.Any<string>(), ContentType.Mod, Arg.Any<DuplicateResolution>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new PlaceResult("placed.jar", 1_000_000, false)));

        var repo = Substitute.For<IInstalledContentRepository>();
        repo.FindAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((InstalledContent?)null);

        var settings = Substitute.For<ISettingsStore>();
        settings.Current.Returns(new LodestoneSettings());

        var useCase = new InstallFromCatalogUseCase(registry, new VersionResolver(), downloader, installer, repo, settings);

        Result<CatalogInstall> result =
            await useCase.ExecuteAsync(Sodium(), GameVersion.Parse("1.21.4"), Loader.Fabric);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Item.ProjectId.ShouldBe("sodium");
        result.Value.InstalledDependencies.ShouldContain("Fabric API");
        await repo.Received(1).UpsertAsync(Arg.Is<InstalledContent>(c => c.Id == "sodium"), Arg.Any<CancellationToken>());
        await repo.Received(1).UpsertAsync(Arg.Is<InstalledContent>(c => c.Id == "fabric-api"), Arg.Any<CancellationToken>());
    }
}

public class RefreshUpdatesUseCaseTests
{
    private static ProjectVersion NewerBuild() => new(
        "v2", "iris", "1.8.1", ContentType.Mod,
        [GameVersion.Parse("1.21.4")], [Loader.Fabric], [], "iris-1.8.1.jar", "https://cdn/iris", "hash", 3.1);

    private static (RefreshUpdatesUseCase UseCase, IUpdateContentUseCase Update, InstalledContent Item)
        Build(bool autoUpdate)
    {
        var item = Make.Mod("iris", projectId: "iris", versions: ["1.21.4"]);
        item.Version = "1.8.0";
        item.Source = "modrinth";

        IReadOnlyList<InstalledContent> all = [item];
        var repo = Substitute.For<IInstalledContentRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(all);

        var source = Substitute.For<IModSource>();
        source.IsConfigured.Returns(true);
        source.GetVersionsAsync("iris", Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<ProjectVersion>>([NewerBuild()]));

        var registry = Substitute.For<IModSourceRegistry>();
        registry.Find("modrinth").Returns(source);

        var update = Substitute.For<IUpdateContentUseCase>();
        update.ApplyAsync(Arg.Any<InstalledContent>(), Arg.Any<ProjectVersion>(), Arg.Any<IProgress<TransferProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        var settings = Substitute.For<ISettingsStore>();
        settings.Current.Returns(new LodestoneSettings { AutoUpdate = autoUpdate });

        return (new RefreshUpdatesUseCase(repo, registry, new VersionResolver(), update, settings), update, item);
    }

    [Fact]
    public async Task Flags_available_updates_when_auto_update_is_off()
    {
        (RefreshUpdatesUseCase useCase, IUpdateContentUseCase update, InstalledContent item) = Build(autoUpdate: false);

        Result<UpdateSummary> result = await useCase.ExecuteAsync(GameVersion.Parse("1.21.4"));

        result.Value.UpdatesAvailable.ShouldBe(1);
        result.Value.Updated.ShouldBe(0);
        item.UpdateAvailable.ShouldBeTrue();
        await update.DidNotReceive().ApplyAsync(Arg.Any<InstalledContent>(), Arg.Any<ProjectVersion>(), Arg.Any<IProgress<TransferProgress>?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Applies_updates_when_auto_update_is_on()
    {
        (RefreshUpdatesUseCase useCase, IUpdateContentUseCase update, _) = Build(autoUpdate: true);

        Result<UpdateSummary> result = await useCase.ExecuteAsync(GameVersion.Parse("1.21.4"));

        result.Value.Updated.ShouldBe(1);
        await update.Received(1).ApplyAsync(Arg.Any<InstalledContent>(), Arg.Any<ProjectVersion>(), Arg.Any<IProgress<TransferProgress>?>(), Arg.Any<CancellationToken>());
    }
}

public class ToggleAndUninstallTests
{
    [Fact]
    public async Task Toggle_flips_state_and_updates_filename_from_installer()
    {
        var item = Make.Mod("sodium");
        item.Enabled = true;
        item.FileName = "sodium.jar";

        var repo = Substitute.For<IInstalledContentRepository>();
        repo.FindAsync("sodium", Arg.Any<CancellationToken>()).Returns(item);
        var installer = Substitute.For<IContentInstaller>();
        installer.SetEnabledAsync(ContentType.Mod, "sodium.jar", false, Arg.Any<CancellationToken>())
            .Returns(Result.Success("sodium.jar.disabled"));

        Result result = await new ToggleContentUseCase(repo, installer).ExecuteAsync("sodium");

        result.IsSuccess.ShouldBeTrue();
        item.Enabled.ShouldBeFalse();
        item.FileName.ShouldBe("sodium.jar.disabled");
    }

    [Fact]
    public async Task Uninstall_removes_the_file_then_the_record()
    {
        var item = Make.Mod("sodium");
        item.FileName = "sodium.jar";
        var repo = Substitute.For<IInstalledContentRepository>();
        repo.FindAsync("sodium", Arg.Any<CancellationToken>()).Returns(item);
        var installer = Substitute.For<IContentInstaller>();
        installer.RemoveAsync(ContentType.Mod, "sodium.jar", Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        Result result = await new UninstallContentUseCase(repo, installer).ExecuteAsync("sodium");

        result.IsSuccess.ShouldBeTrue();
        await repo.Received(1).RemoveAsync("sodium", Arg.Any<CancellationToken>());
    }
}

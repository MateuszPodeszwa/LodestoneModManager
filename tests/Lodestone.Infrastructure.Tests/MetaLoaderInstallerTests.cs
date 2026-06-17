using System.IO;
using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using Lodestone.Infrastructure.FileSystem;
using Lodestone.Infrastructure.Loaders;
using Lodestone.Infrastructure.Persistence;
using RichardSzalay.MockHttp;

namespace Lodestone.Infrastructure.Tests;

public class MetaLoaderInstallerTests
{
    private static async Task<(MetaLoaderInstaller Installer, string GameDir)> BuildAsync(
        TempDir dir, MockHttpMessageHandler? mock = null, string? installedVersion = "1.21.4")
    {
        string gameDir = dir.File("game");
        Directory.CreateDirectory(Path.Combine(gameDir, "mods")); // makes the locator treat it as valid
        if (installedVersion is not null)
        {
            SeedVanilla(gameDir, installedVersion); // base version the loader inherits from must exist
        }

        var settings = new JsonSettingsStore(dir.File("settings.json"));
        await settings.SaveAsync(new LodestoneSettings { GameDirectory = gameDir });
        HttpClient http = (mock ?? new MockHttpMessageHandler()).ToHttpClient();
        var inventory = new MinecraftGameInventory(settings);
        return (new MetaLoaderInstaller(http, settings, new MinecraftGameLocator(), inventory), gameDir);
    }

    private static void SeedVanilla(string gameDir, string version)
    {
        string dir = Path.Combine(gameDir, "versions", version);
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, version + ".json"), $$"""{ "id": "{{version}}", "type": "release" }""");
    }

    [Fact]
    public async Task Supports_only_fabric_and_quilt()
    {
        using var dir = new TempDir();
        (MetaLoaderInstaller installer, _) = await BuildAsync(dir);

        installer.Supports(Loader.Fabric).ShouldBeTrue();
        installer.Supports(Loader.Quilt).ShouldBeTrue();
        installer.Supports(Loader.Forge).ShouldBeFalse();
        installer.Supports(Loader.NeoForge).ShouldBeFalse();
    }

    [Fact]
    public async Task Forge_reports_unsupported_with_guidance()
    {
        using var dir = new TempDir();
        (MetaLoaderInstaller installer, _) = await BuildAsync(dir);

        Result result = await installer.EnsureInstalledAsync(Loader.Forge, GameVersion.Parse("1.21.4"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("loader.unsupported");
    }

    [Fact]
    public async Task Refuses_when_the_base_game_version_is_not_installed()
    {
        using var dir = new TempDir();
        (MetaLoaderInstaller installer, _) = await BuildAsync(dir, installedVersion: null);

        Result result = await installer.EnsureInstalledAsync(Loader.Fabric, GameVersion.Parse("1.21.4"));

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("loader.base_missing");
    }

    [Fact]
    public async Task Installs_fabric_profile_and_launcher_entry()
    {
        using var dir = new TempDir();
        var mock = new MockHttpMessageHandler();
        mock.When("https://meta.fabricmc.net/v2/versions/loader/1.21.4")
            .Respond("application/json", """[{ "loader": { "version": "0.16.5", "stable": true } }]""");
        mock.When("https://meta.fabricmc.net/v2/versions/loader/1.21.4/0.16.5/profile/json")
            .Respond("application/json", """{ "id": "fabric-loader-0.16.5-1.21.4", "inheritsFrom": "1.21.4" }""");

        (MetaLoaderInstaller installer, string gameDir) = await BuildAsync(dir, mock);

        installer.IsInstalled(Loader.Fabric, GameVersion.Parse("1.21.4")).ShouldBeFalse();

        Result result = await installer.EnsureInstalledAsync(Loader.Fabric, GameVersion.Parse("1.21.4"));

        result.IsSuccess.ShouldBeTrue();
        string profile = Path.Combine(gameDir, "versions", "fabric-loader-0.16.5-1.21.4", "fabric-loader-0.16.5-1.21.4.json");
        File.Exists(profile).ShouldBeTrue();

        string launcher = Path.Combine(gameDir, "launcher_profiles.json");
        File.Exists(launcher).ShouldBeTrue();
        (await File.ReadAllTextAsync(launcher)).ShouldContain("fabric-loader-0.16.5-1.21.4");

        installer.IsInstalled(Loader.Fabric, GameVersion.Parse("1.21.4")).ShouldBeTrue();
    }

    [Fact]
    public async Task Update_installs_the_latest_when_no_loader_is_present()
    {
        using var dir = new TempDir();
        var mock = new MockHttpMessageHandler();
        mock.When("https://meta.fabricmc.net/v2/versions/loader/1.21.4")
            .Respond("application/json", """[{ "loader": { "version": "0.16.5", "stable": true } }]""");
        mock.When("https://meta.fabricmc.net/v2/versions/loader/1.21.4/0.16.5/profile/json")
            .Respond("application/json", """{ "id": "fabric-loader-0.16.5-1.21.4", "inheritsFrom": "1.21.4" }""");

        (MetaLoaderInstaller installer, string gameDir) = await BuildAsync(dir, mock);

        Result<LoaderUpdate> result = await installer.UpdateAsync(Loader.Fabric, GameVersion.Parse("1.21.4"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Changed.ShouldBeTrue();
        result.Value.PreviousVersion.ShouldBeNull();
        result.Value.Version.ShouldBe("0.16.5");
        File.Exists(Path.Combine(gameDir, "versions", "fabric-loader-0.16.5-1.21.4", "fabric-loader-0.16.5-1.21.4.json")).ShouldBeTrue();
    }

    [Fact]
    public async Task Update_upgrades_in_place_when_a_newer_build_exists()
    {
        using var dir = new TempDir();
        var mock = new MockHttpMessageHandler();
        mock.When("https://meta.fabricmc.net/v2/versions/loader/1.21.4")
            .Respond("application/json", """[{ "loader": { "version": "0.16.5", "stable": true } }]""");
        mock.When("https://meta.fabricmc.net/v2/versions/loader/1.21.4/0.16.5/profile/json")
            .Respond("application/json", """{ "id": "fabric-loader-0.16.5-1.21.4", "inheritsFrom": "1.21.4" }""");

        (MetaLoaderInstaller installer, string gameDir) = await BuildAsync(dir, mock);
        Directory.CreateDirectory(Path.Combine(gameDir, "versions", "fabric-loader-0.16.0-1.21.4"));

        Result<LoaderUpdate> result = await installer.UpdateAsync(Loader.Fabric, GameVersion.Parse("1.21.4"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Changed.ShouldBeTrue();
        result.Value.PreviousVersion.ShouldBe("0.16.0");
        result.Value.Version.ShouldBe("0.16.5");
        File.Exists(Path.Combine(gameDir, "versions", "fabric-loader-0.16.5-1.21.4", "fabric-loader-0.16.5-1.21.4.json")).ShouldBeTrue();
    }

    [Fact]
    public async Task Update_is_a_noop_when_already_on_the_latest()
    {
        using var dir = new TempDir();
        var mock = new MockHttpMessageHandler();
        mock.When("https://meta.fabricmc.net/v2/versions/loader/1.21.4")
            .Respond("application/json", """[{ "loader": { "version": "0.16.5", "stable": true } }]""");

        (MetaLoaderInstaller installer, string gameDir) = await BuildAsync(dir, mock);
        Directory.CreateDirectory(Path.Combine(gameDir, "versions", "fabric-loader-0.16.5-1.21.4"));

        Result<LoaderUpdate> result = await installer.UpdateAsync(Loader.Fabric, GameVersion.Parse("1.21.4"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.Changed.ShouldBeFalse();
        result.Value.Version.ShouldBe("0.16.5");
    }

    [Fact]
    public async Task InstalledVersion_reports_the_newest_build_present()
    {
        using var dir = new TempDir();
        (MetaLoaderInstaller installer, string gameDir) = await BuildAsync(dir);
        Directory.CreateDirectory(Path.Combine(gameDir, "versions", "fabric-loader-0.16.0-1.21.4"));
        Directory.CreateDirectory(Path.Combine(gameDir, "versions", "fabric-loader-0.16.5-1.21.4"));

        installer.InstalledVersion(Loader.Fabric, GameVersion.Parse("1.21.4")).ShouldBe("0.16.5");
    }

    [Fact]
    public async Task RemoveManaged_deletes_fabric_quilt_but_keeps_vanilla_and_forge()
    {
        using var dir = new TempDir();
        (MetaLoaderInstaller installer, string gameDir) = await BuildAsync(dir); // seeds vanilla 1.21.4
        Directory.CreateDirectory(Path.Combine(gameDir, "versions", "fabric-loader-0.16.5-1.21.4"));
        Directory.CreateDirectory(Path.Combine(gameDir, "versions", "quilt-loader-0.26.0-1.21.4"));
        Directory.CreateDirectory(Path.Combine(gameDir, "versions", "1.20.1-forge-47.2.0")); // user's own installer

        Result<int> result = await installer.RemoveManagedAsync();

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(2);
        Directory.Exists(Path.Combine(gameDir, "versions", "fabric-loader-0.16.5-1.21.4")).ShouldBeFalse();
        Directory.Exists(Path.Combine(gameDir, "versions", "quilt-loader-0.26.0-1.21.4")).ShouldBeFalse();
        Directory.Exists(Path.Combine(gameDir, "versions", "1.20.1-forge-47.2.0")).ShouldBeTrue(); // left alone
        Directory.Exists(Path.Combine(gameDir, "versions", "1.21.4")).ShouldBeTrue(); // vanilla kept
    }
}

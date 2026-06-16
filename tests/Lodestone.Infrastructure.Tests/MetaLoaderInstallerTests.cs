using System.IO;
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
    private static async Task<(MetaLoaderInstaller Installer, string GameDir)> BuildAsync(TempDir dir, MockHttpMessageHandler? mock = null)
    {
        string gameDir = dir.File("game");
        Directory.CreateDirectory(Path.Combine(gameDir, "mods")); // makes the locator treat it as valid
        var settings = new JsonSettingsStore(dir.File("settings.json"));
        await settings.SaveAsync(new LodestoneSettings { GameDirectory = gameDir });
        HttpClient http = (mock ?? new MockHttpMessageHandler()).ToHttpClient();
        return (new MetaLoaderInstaller(http, settings, new MinecraftGameLocator()), gameDir);
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
}

using System.IO;
using System.Text.Json.Nodes;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using Lodestone.Infrastructure.FileSystem;
using Lodestone.Infrastructure.Persistence;

namespace Lodestone.Infrastructure.Tests;

public class LauncherProfileStoreTests
{
    private const string Launcher = """
    {
      "profiles": {
        "vanilla": { "name": "1.20.1", "type": "latest-release", "lastVersionId": "1.20.1" },
        "fab": { "name": "Fabric", "type": "custom", "lastVersionId": "fabric-loader-0.16.5-1.20.1", "javaArgs": "-Xmx4G" },
        "forge": { "name": "Forge", "type": "custom", "lastVersionId": "1.19.2-forge-43.2.0" }
      },
      "version": 3
    }
    """;

    private static readonly LoaderProfile Fabric = new(GameVersion.Parse("1.20.1"), Loader.Fabric, "fabric-loader-0.16.5-1.20.1");
    private static readonly LoaderProfile Forge = new(GameVersion.Parse("1.19.2"), Loader.Forge, "1.19.2-forge-43.2.0");

    private static async Task<(LauncherProfileStore Store, string GameDir, string Stash)> BuildAsync(TempDir dir)
    {
        string gameDir = dir.File("game");
        Directory.CreateDirectory(gameDir);
        File.WriteAllText(Path.Combine(gameDir, "launcher_profiles.json"), Launcher);
        var settings = new JsonSettingsStore(dir.File("settings.json"));
        await settings.SaveAsync(new LodestoneSettings { GameDirectory = gameDir });
        return (new LauncherProfileStore(settings, dir.File("stash.json")), gameDir, dir.File("stash.json"));
    }

    private static JsonObject Profiles(string gameDir)
        => (JsonObject)JsonNode.Parse(File.ReadAllText(Path.Combine(gameDir, "launcher_profiles.json")))!["profiles"]!;

    [Fact]
    public async Task Apply_hides_other_modded_profiles_but_keeps_vanilla_and_active()
    {
        using var dir = new TempDir();
        (LauncherProfileStore store, string gameDir, string stash) = await BuildAsync(dir);

        Result result = store.Apply(Fabric, [Fabric, Forge]);

        result.IsSuccess.ShouldBeTrue();
        JsonObject profiles = Profiles(gameDir);
        profiles.ContainsKey("vanilla").ShouldBeTrue(); // vanilla untouched
        profiles.ContainsKey("fab").ShouldBeTrue();     // active kept
        profiles.ContainsKey("forge").ShouldBeFalse();  // other modded hidden
        ((JsonObject)JsonNode.Parse(File.ReadAllText(stash))!).ContainsKey("forge").ShouldBeTrue();
    }

    [Fact]
    public async Task Switching_back_restores_a_hidden_profile_with_its_custom_settings()
    {
        using var dir = new TempDir();
        (LauncherProfileStore store, string gameDir, _) = await BuildAsync(dir);

        store.Apply(Forge, [Fabric, Forge]);  // hides fabric (which carries javaArgs)
        store.Apply(Fabric, [Fabric, Forge]); // switch back

        JsonObject profiles = Profiles(gameDir);
        profiles.ContainsKey("fab").ShouldBeTrue();
        profiles["fab"]!["javaArgs"]!.GetValue<string>().ShouldBe("-Xmx4G"); // custom settings survived the round trip
        profiles.ContainsKey("forge").ShouldBeFalse(); // forge now hidden
    }

    [Fact]
    public async Task RestoreAll_brings_every_hidden_profile_back_and_clears_the_stash()
    {
        using var dir = new TempDir();
        (LauncherProfileStore store, string gameDir, string stash) = await BuildAsync(dir);

        store.Apply(Fabric, [Fabric, Forge]); // hides forge
        Result result = store.RestoreAll();

        result.IsSuccess.ShouldBeTrue();
        JsonObject profiles = Profiles(gameDir);
        profiles.ContainsKey("forge").ShouldBeTrue(); // back
        profiles.ContainsKey("fab").ShouldBeTrue();
        File.Exists(stash).ShouldBeFalse();           // stash cleared
    }
}

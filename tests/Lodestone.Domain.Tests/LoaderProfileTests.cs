namespace Lodestone.Domain.Tests;

public class LoaderProfileTests
{
    private static LoaderProfile Profile(Loader loader, string gameVersion, string versionId)
        => new(GameVersion.Parse(gameVersion), loader, versionId);

    [Theory]
    [InlineData(Loader.Fabric, "1.21.4", "fabric-loader-0.16.5-1.21.4", "0.16.5")]
    [InlineData(Loader.Quilt, "1.21.4", "quilt-loader-0.26.4-1.21.4", "0.26.4")]
    [InlineData(Loader.Forge, "1.20.1", "1.20.1-forge-47.2.0", "47.2.0")]
    [InlineData(Loader.NeoForge, "1.21.1", "neoforge-21.1.65", "21.1.65")]
    public void LoaderVersion_is_parsed_from_the_profile_id(Loader loader, string mc, string id, string expected)
    {
        Profile(loader, mc, id).LoaderVersion.ShouldBe(expected);
    }

    [Fact]
    public void LoaderVersion_is_null_for_vanilla_and_unrecognised_ids()
    {
        Profile(Loader.None, "1.21.4", "1.21.4").LoaderVersion.ShouldBeNull();
        Profile(Loader.Fabric, "1.21.4", "totally-unexpected").LoaderVersion.ShouldBeNull();
    }

    [Theory]
    [InlineData(Loader.Fabric, "1.21.4", "fabric-loader-0.16.5-1.21.4", "Fabric loader 0.16.5 · MC 1.21.4")]
    [InlineData(Loader.Quilt, "1.21.4", "quilt-loader-0.26.4-1.21.4", "Quilt loader 0.26.4 · MC 1.21.4")]
    [InlineData(Loader.Forge, "1.20.1", "1.20.1-forge-47.2.0", "Forge 47.2.0 · MC 1.20.1")]
    [InlineData(Loader.NeoForge, "1.21.1", "neoforge-21.1.65", "NeoForge 21.1.65 · MC 1.21.1")]
    public void PreciseLabel_reads_as_a_friendly_build_description(Loader loader, string mc, string id, string expected)
    {
        Profile(loader, mc, id).PreciseLabel.ShouldBe(expected);
    }

    [Fact]
    public void PreciseLabel_falls_back_to_the_loader_name_when_the_build_cant_be_isolated()
    {
        // Unparseable id: keep the loader name + MC version, drop the unknown build number rather than garble it.
        Profile(Loader.Fabric, "1.21.4", "weird-folder-name").PreciseLabel.ShouldBe("Fabric · MC 1.21.4");
    }

    [Fact]
    public void PreciseLabel_is_just_the_version_for_vanilla()
    {
        Profile(Loader.None, "1.21.4", "1.21.4").PreciseLabel.ShouldBe("1.21.4");
    }
}

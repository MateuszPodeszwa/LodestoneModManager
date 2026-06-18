using Lodestone.Domain;

namespace Lodestone.Domain.Tests;

public class InstalledContentProfileTests
{
    [Theory]
    [InlineData(Loader.Fabric, Loader.Fabric, true)]
    [InlineData(Loader.Fabric, Loader.Quilt, false)]
    [InlineData(Loader.Quilt, Loader.Fabric, false)]
    [InlineData(Loader.Forge, Loader.NeoForge, false)]
    public void A_mod_matches_only_its_own_loader_profile(Loader modLoader, Loader activeLoader, bool expected)
    {
        var mod = new InstalledContent("sodium", "Sodium", ContentType.Mod) { Loader = modLoader };

        mod.MatchesLoaderProfile(activeLoader).ShouldBe(expected);
    }

    [Theory]
    [InlineData(Loader.Fabric)]
    [InlineData(Loader.Quilt)]
    [InlineData(Loader.None)]
    public void Loader_independent_content_matches_any_profile(Loader activeLoader)
    {
        var pack = new InstalledContent("faithful", "Faithful", ContentType.ResourcePack) { Loader = Loader.None };
        var shader = new InstalledContent("complementary", "Complementary", ContentType.Shader) { Loader = Loader.None };

        pack.MatchesLoaderProfile(activeLoader).ShouldBeTrue();
        shader.MatchesLoaderProfile(activeLoader).ShouldBeTrue();
    }

    [Fact]
    public void A_mod_serves_a_profile_only_on_a_matching_loader_and_version()
    {
        var mod = new InstalledContent("dynamic-lights", "Dynamic Lights", ContentType.Mod)
        {
            Loader = Loader.Fabric,
            GameVersions = [GameVersion.Parse("26.2")],
        };

        mod.ServesProfile(Loader.Fabric, GameVersion.Parse("26.2")).ShouldBeTrue();   // its own profile
        mod.ServesProfile(Loader.Fabric, GameVersion.Parse("1.21.9")).ShouldBeFalse(); // same loader, other version
        mod.ServesProfile(Loader.Forge, GameVersion.Parse("26.2")).ShouldBeFalse();    // other loader
        mod.ServesProfile(Loader.Fabric, null).ShouldBeTrue();                          // no concrete version → loader only
    }

    [Fact]
    public void Loader_independent_content_serves_any_profile_regardless_of_version()
    {
        var pack = new InstalledContent("faithful", "Faithful", ContentType.ResourcePack) { Loader = Loader.None };

        pack.ServesProfile(Loader.Fabric, GameVersion.Parse("1.21.9")).ShouldBeTrue();
        pack.ServesProfile(Loader.None, null).ShouldBeTrue();
    }
}

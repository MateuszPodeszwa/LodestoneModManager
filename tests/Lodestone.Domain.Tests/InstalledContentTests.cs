namespace Lodestone.Domain.Tests;

public class InstalledContentTests
{
    private static InstalledContent NewSodium() => new("sodium", "Sodium", ContentType.Mod)
    {
        Author = "CaffeineMC",
        Version = "0.5.8",
        Loader = Loader.Fabric,
        ProjectId = "AANobbMI",
        Source = "modrinth",
        GameVersions = [GameVersion.Parse("1.21.4"), GameVersion.Parse("1.20.1")],
        ProvidedIds = ["sodium"],
    };

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Construction_requires_a_non_blank_id(string badId)
    {
        Should.Throw<ArgumentException>(() => new InstalledContent(badId, "Name", ContentType.Mod));
    }

    [Fact]
    public void Construction_requires_a_non_blank_name()
    {
        Should.Throw<ArgumentException>(() => new InstalledContent("id", "  ", ContentType.Mod));
    }

    [Fact]
    public void SupportsVersion_matches_declared_versions()
    {
        var sodium = NewSodium();

        sodium.SupportsVersion(GameVersion.Parse("1.21.4")).ShouldBeTrue();
        sodium.SupportsVersion(GameVersion.Parse("1.19.2")).ShouldBeFalse();
    }

    [Fact]
    public void Provides_matches_id_projectId_and_provided_ids_case_insensitively()
    {
        var sodium = NewSodium();

        sodium.Provides("sodium").ShouldBeTrue();
        sodium.Provides("SODIUM").ShouldBeTrue();
        sodium.Provides("AANobbMI").ShouldBeTrue();   // project id
        sodium.Provides("fabric-api").ShouldBeFalse();
        sodium.Provides("").ShouldBeFalse();
    }

    [Fact]
    public void Defaults_are_sensible_for_a_fresh_item()
    {
        var item = new InstalledContent("x", "X", ContentType.ResourcePack);

        item.Enabled.ShouldBeTrue();
        item.Loader.ShouldBe(Loader.None);
        item.Source.ShouldBe("local");
        item.GameVersions.ShouldBeEmpty();
        item.Dependencies.ShouldBeEmpty();
    }
}

public class DependencyTests
{
    [Fact]
    public void Label_falls_back_to_identifier_when_no_display_name()
    {
        new Dependency("fabric-api", DependencyKind.Required).Label.ShouldBe("fabric-api");
        new Dependency("P7dR8mSH", DependencyKind.Required, DisplayName: "Fabric API").Label.ShouldBe("Fabric API");
    }
}

namespace Lodestone.Domain.Tests;

public class LoaderTests
{
    [Theory]
    [InlineData("fabric", Loader.Fabric)]
    [InlineData("FABRIC", Loader.Fabric)]
    [InlineData(" Forge ", Loader.Forge)]
    [InlineData("quilt", Loader.Quilt)]
    [InlineData("neoforge", Loader.NeoForge)]
    [InlineData("something-else", Loader.None)]
    [InlineData("", Loader.None)]
    [InlineData(null, Loader.None)]
    public void ParseLoader_is_tolerant(string? input, Loader expected)
    {
        input.ParseLoader().ShouldBe(expected);
    }

    [Fact]
    public void ToSlug_and_DisplayName_round_trip_meaningfully()
    {
        Loader.Fabric.ToSlug().ShouldBe("fabric");
        Loader.NeoForge.ToSlug().ShouldBe("neoforge");
        Loader.None.ToSlug().ShouldBe(string.Empty);
        Loader.Quilt.ToDisplayName().ShouldBe("Quilt");
    }
}

public class ContentTypeTests
{
    [Theory]
    [InlineData(ContentType.Mod, true)]
    [InlineData(ContentType.ResourcePack, false)]
    [InlineData(ContentType.Shader, false)]
    public void Only_mods_use_a_loader(ContentType type, bool expected)
    {
        type.UsesLoader().ShouldBe(expected);
    }

    [Theory]
    [InlineData(ContentType.Mod, "mods")]
    [InlineData(ContentType.ResourcePack, "resourcepacks")]
    [InlineData(ContentType.Shader, "shaderpacks")]
    public void Folder_name_matches_minecraft_conventions(ContentType type, string folder)
    {
        type.ToFolderName().ShouldBe(folder);
    }
}

using Lodestone.Application.Catalog;
using Lodestone.Domain;

namespace Lodestone.Application.Tests;

public class VersionComparerTests
{
    [Theory]
    [InlineData("0.5.8", "0.5.7", true)]
    [InlineData("0.5.8", "0.5.8", false)]
    [InlineData("1.8.0", "1.8.1", false)]
    [InlineData("19.5.0", "19.4.0", true)]
    [InlineData("r5.3", "r5.3", false)]
    [InlineData("2.1.0", "2.0.9", true)]
    public void IsNewer_compares_leading_numbers(string candidate, string current, bool expected)
    {
        VersionComparer.IsNewer(candidate, current).ShouldBe(expected);
    }
}

public class VersionResolverTests
{
    private static ProjectVersion Pv(
        string number,
        string[] games,
        Loader[] loaders,
        DateTimeOffset? published = null)
        => new(
            Id: "v-" + number,
            ProjectId: "proj",
            VersionNumber: number,
            Type: ContentType.Mod,
            GameVersions: games.Select(GameVersion.Parse).ToList(),
            Loaders: loaders.ToList(),
            Dependencies: [],
            FileName: number + ".jar",
            DownloadUrl: "https://example/" + number,
            Sha512: null,
            SizeMb: 1.0,
            Published: published);

    private readonly VersionResolver _resolver = new();

    [Fact]
    public void Resolves_the_newest_build_matching_version_and_loader()
    {
        var versions = new[]
        {
            Pv("0.5.6", ["1.21.4"], [Loader.Fabric], DateTimeOffset.Parse("2024-01-01")),
            Pv("0.5.8", ["1.21.4"], [Loader.Fabric], DateTimeOffset.Parse("2024-06-01")),
            Pv("0.5.9", ["1.20.1"], [Loader.Fabric], DateTimeOffset.Parse("2024-07-01")),
        };

        ProjectVersion? chosen = _resolver.Resolve(versions, GameVersion.Parse("1.21.4"), Loader.Fabric);

        chosen.ShouldNotBeNull();
        chosen!.VersionNumber.ShouldBe("0.5.8");
    }

    [Fact]
    public void Returns_null_when_no_build_matches_the_loader()
    {
        var versions = new[] { Pv("1.0.0", ["1.21.4"], [Loader.Forge]) };

        _resolver.Resolve(versions, GameVersion.Parse("1.21.4"), Loader.Fabric).ShouldBeNull();
    }

    [Fact]
    public void Loaderless_content_matches_any_loader()
    {
        var versions = new[] { Pv("32x", ["1.21.4"], []) };

        _resolver.Resolve(versions, GameVersion.Parse("1.21.4"), Loader.None).ShouldNotBeNull();
    }
}

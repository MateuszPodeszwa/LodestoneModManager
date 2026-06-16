namespace Lodestone.Domain.Tests;

public class GameVersionTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_rejects_blank_input(string? raw)
    {
        var result = GameVersion.Create(raw);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("game_version.empty");
    }

    [Theory]
    [InlineData("1.21.4")]
    [InlineData("1.20.1")]
    [InlineData("1.21")]
    public void Create_parses_release_versions(string raw)
    {
        var version = GameVersion.Create(raw).Value;

        version.Value.ShouldBe(raw);
        version.IsRelease.ShouldBeTrue();
    }

    [Fact]
    public void Create_trims_surrounding_whitespace()
    {
        GameVersion.Create("  1.21.4  ").Value.Value.ShouldBe("1.21.4");
    }

    [Fact]
    public void Snapshots_are_preserved_but_not_marked_as_releases()
    {
        var snapshot = GameVersion.Create("24w44a").Value;

        snapshot.Value.ShouldBe("24w44a");
        snapshot.IsRelease.ShouldBeFalse();
    }

    [Fact]
    public void Equality_is_case_insensitive_on_the_raw_value()
    {
        GameVersion.Parse("1.21.4").ShouldBe(GameVersion.Parse("1.21.4"));
        (GameVersion.Parse("1.21.4") == GameVersion.Parse("1.21.4")).ShouldBeTrue();
        (GameVersion.Parse("1.21.4") == GameVersion.Parse("1.20.1")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("1.21.4", "1.20.1", 1)]
    [InlineData("1.20.1", "1.21.4", -1)]
    [InlineData("1.21.1", "1.21", 1)]
    [InlineData("1.21", "1.21.0", 0)]
    [InlineData("1.21.4", "1.21.4", 0)]
    public void CompareTo_orders_releases_componentwise(string left, string right, int expectedSign)
    {
        Math.Sign(GameVersion.Parse(left).CompareTo(GameVersion.Parse(right))).ShouldBe(expectedSign);
    }

    [Fact]
    public void Releases_sort_above_snapshots()
    {
        (GameVersion.Parse("1.21.4") > GameVersion.Parse("24w44a")).ShouldBeTrue();
    }

    [Fact]
    public void Parse_throws_on_blank()
    {
        Should.Throw<FormatException>(() => GameVersion.Parse(" "));
    }
}

using Lodestone.Application.Abstractions;
using Lodestone.Application.Settings;
using Lodestone.Domain;
using NSubstitute;

namespace Lodestone.Application.Tests;

public class ActiveProfileTests
{
    private static IGameInventory Inventory(params string[] installed)
    {
        var inv = Substitute.For<IGameInventory>();
        inv.InstalledVersions().Returns(installed.Select(GameVersion.Parse).ToList());
        return inv;
    }

    [Theory]
    [InlineData("all")]
    [InlineData("")]
    public void Selected_is_null_on_the_all_versions_view(string selected)
        => ActiveProfile.Selected(new LodestoneSettings { SelectedVersion = selected }).ShouldBeNull();

    [Fact]
    public void Selected_returns_the_concrete_version()
        => ActiveProfile.Selected(new LodestoneSettings { SelectedVersion = "1.20.1" })!.Value.ShouldBe("1.20.1");

    [Fact]
    public void Target_uses_the_explicit_selection_when_set()
    {
        var settings = new LodestoneSettings { SelectedVersion = "1.20.1" };

        ActiveProfile.Target(settings, Inventory("1.21.4", "1.20.1"))!.Value.ShouldBe("1.20.1");
    }

    [Fact]
    public void Target_falls_back_to_the_newest_installed_version_on_the_all_versions_view()
    {
        var settings = new LodestoneSettings { SelectedVersion = "all" };

        // InstalledVersions is newest-first, so the first entry is the newest.
        ActiveProfile.Target(settings, Inventory("1.21.4", "1.20.1"))!.Value.ShouldBe("1.21.4");
    }

    [Fact]
    public void Target_is_null_when_all_is_selected_and_nothing_is_installed()
    {
        var settings = new LodestoneSettings { SelectedVersion = "all" };

        ActiveProfile.Target(settings, Inventory()).ShouldBeNull();
    }
}

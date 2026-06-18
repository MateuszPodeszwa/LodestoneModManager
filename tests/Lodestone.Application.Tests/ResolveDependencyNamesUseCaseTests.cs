using Lodestone.Application.Abstractions;
using Lodestone.Application.Catalog;
using Lodestone.Application.UseCases;
using Lodestone.Domain;
using Lodestone.Domain.Common;
using NSubstitute;

namespace Lodestone.Application.Tests;

public class ResolveDependencyNamesUseCaseTests
{
    private static CatalogProject Project(string id, string name) => new(
        id, id, name, "author", ContentType.Mod, "desc", 0, 0, [], [Loader.Fabric], [], "modrinth");

    private static (ResolveDependencyNamesUseCase UseCase, IModSource Source, IInstalledContentRepository Repo) Build(
        params InstalledContent[] items)
    {
        var source = Substitute.For<IModSource>();
        source.Name.Returns("modrinth");
        source.IsConfigured.Returns(true);

        var registry = Substitute.For<IModSourceRegistry>();
        registry.Primary.Returns(source);
        registry.Find("modrinth").Returns(source);

        var repo = Substitute.For<IInstalledContentRepository>();
        repo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(items);

        return (new ResolveDependencyNamesUseCase(registry, repo), source, repo);
    }

    [Fact]
    public async Task Resolves_and_persists_a_missing_dependency_name()
    {
        InstalledContent mod = Make.Mod("more-culling", projectId: "more-culling", deps: [Make.Requires("9s6osm5g")]);
        (ResolveDependencyNamesUseCase useCase, IModSource source, IInstalledContentRepository repo) = Build(mod);
        source.GetProjectAsync("9s6osm5g", Arg.Any<CancellationToken>()).Returns(Result.Success(Project("9s6osm5g", "Cloth Config")));

        int updated = await useCase.ExecuteAsync();

        updated.ShouldBe(1);
        mod.Dependencies[0].DisplayName.ShouldBe("Cloth Config");
        mod.Dependencies[0].Label.ShouldBe("Cloth Config");
        await repo.Received(1).UpsertAsync(mod, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_nothing_when_every_dependency_already_has_a_name()
    {
        InstalledContent mod = Make.Mod("more-culling", projectId: "more-culling",
            deps: [new Dependency("9s6osm5g", DependencyKind.Required, DisplayName: "Cloth Config")]);
        (ResolveDependencyNamesUseCase useCase, IModSource source, IInstalledContentRepository repo) = Build(mod);

        int updated = await useCase.ExecuteAsync();

        updated.ShouldBe(0);
        await source.DidNotReceive().GetProjectAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await repo.DidNotReceive().UpsertAsync(Arg.Any<InstalledContent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Leaves_the_id_untouched_when_the_source_cannot_resolve_it()
    {
        InstalledContent mod = Make.Mod("more-culling", projectId: "more-culling", deps: [Make.Requires("9s6osm5g")]);
        (ResolveDependencyNamesUseCase useCase, IModSource source, IInstalledContentRepository repo) = Build(mod);
        source.GetProjectAsync("9s6osm5g", Arg.Any<CancellationToken>())
            .Returns(Result.Failure<CatalogProject>("source.not_found", "No such project."));

        int updated = await useCase.ExecuteAsync();

        updated.ShouldBe(0);
        mod.Dependencies[0].DisplayName.ShouldBeNull();
        mod.Dependencies[0].Label.ShouldBe("9s6osm5g"); // graceful fallback to the raw id
        await repo.DidNotReceive().UpsertAsync(Arg.Any<InstalledContent>(), Arg.Any<CancellationToken>());
    }
}

using Lodestone.Domain;

namespace Lodestone.App.ViewModels;

/// <summary>
/// Decides whether a catalog search result can actually be installed for the active profile, and the
/// reason when it can't. Browse surfaces a project whenever it matches the search facets, but Modrinth
/// applies those facets across <em>all</em> of a project's files — so a project can appear without
/// having any build for this exact loader + version pair. Both the Browse cards and the detail modal
/// run a result through here, so an unsupported mod is flagged and its install blocked up front,
/// rather than only failing with a toast at install time.
/// </summary>
internal static class CatalogCompatibility
{
    /// <param name="target">The concrete install target (selected version, else newest installed),
    /// or null when nothing is installed yet — in which case nothing is blocked here, since that case
    /// is gated separately on install.</param>
    /// <param name="loader">The active loader; only checked for content that uses one (mods).</param>
    public static (bool Compatible, string? Reason) Evaluate(CatalogProject project, GameVersion? target, Loader loader)
    {
        if (target is null)
        {
            return (true, null); // no concrete target yet — gated separately on install
        }

        if (!project.SupportsVersion(target))
        {
            return (false, $"{project.Name} doesn't support Minecraft {target} — there's no build for this version.");
        }

        if (project.Type.UsesLoader() && loader != Loader.None && !project.SupportsLoader(loader))
        {
            return (false, $"{project.Name} has no {loader.ToDisplayName()} build for Minecraft {target}.");
        }

        return (true, null);
    }
}

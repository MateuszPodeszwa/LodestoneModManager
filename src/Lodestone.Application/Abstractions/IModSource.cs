using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

public enum ModSortOrder
{
    Relevance = 0,
    Downloads,
    Followers,
}

/// <summary>Parameters for a catalog search. Optional filters narrow by type, category, version and loader.</summary>
public sealed record ModSearchQuery(
    string Text = "",
    ContentType? Type = null,
    string? Category = null,
    ModSortOrder Sort = ModSortOrder.Relevance,
    GameVersion? GameVersion = null,
    Loader? Loader = null,
    int Offset = 0,
    int Limit = 20);

/// <summary>
/// A browsable mod source (Modrinth, CurseForge…). Strategy: each source is interchangeable behind
/// this port; the registry picks which one(s) to use based on settings.
/// </summary>
public interface IModSource
{
    /// <summary>Stable lowercase identifier, e.g. <c>modrinth</c>.</summary>
    string Name { get; }

    /// <summary>True when the source is usable (e.g. CurseForge requires a configured API key).</summary>
    bool IsConfigured { get; }

    Task<Result<IReadOnlyList<CatalogProject>>> SearchAsync(ModSearchQuery query, CancellationToken ct = default);

    Task<Result<CatalogProject>> GetProjectAsync(string idOrSlug, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ProjectVersion>>> GetVersionsAsync(string projectId, CancellationToken ct = default);
}

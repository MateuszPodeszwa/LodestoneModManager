using Lodestone.Domain;
using Lodestone.Domain.Common;

namespace Lodestone.Application.Abstractions;

/// <summary>
/// Metadata extracted from a local archive without extracting it to disk. Fields are best-effort:
/// a plain resource pack yields little beyond <see cref="Type"/>, while a Fabric/Forge/Quilt mod
/// yields its id, declared loaders and dependencies (which feed the compatibility engine).
/// </summary>
public sealed record LocalContentMetadata(
    ContentType Type,
    string? ModId = null,
    string? Name = null,
    string? Version = null,
    IReadOnlyList<Loader>? Loaders = null,
    IReadOnlyList<Dependency>? Dependencies = null,
    IReadOnlyList<string>? ProvidedIds = null,
    IReadOnlyList<GameVersion>? GameVersions = null)
{
    public IReadOnlyList<Loader> LoadersOrEmpty => Loaders ?? [];
    public IReadOnlyList<Dependency> DependenciesOrEmpty => Dependencies ?? [];
    public IReadOnlyList<string> ProvidedIdsOrEmpty => ProvidedIds ?? [];
    public IReadOnlyList<GameVersion> GameVersionsOrEmpty => GameVersions ?? [];
}

/// <summary>
/// Reads loader metadata from a <c>.jar</c>/<c>.zip</c>/<c>.litemod</c>/<c>.mcpack</c>. The
/// implementation dispatches to per-format parsers (fabric.mod.json / mods.toml / quilt.mod.json) -
/// a Strategy hidden behind this single port.
/// </summary>
public interface IArchiveMetadataReader
{
    Task<Result<LocalContentMetadata>> ReadAsync(string filePath, CancellationToken ct = default);
}

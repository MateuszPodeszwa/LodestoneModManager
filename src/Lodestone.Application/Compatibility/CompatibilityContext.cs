using Lodestone.Domain;

namespace Lodestone.Application.Compatibility;

/// <summary>
/// Inputs to a compatibility scan: the library plus the profile being checked against. A null
/// <see cref="ActiveVersion"/> (the "All versions" view) skips version checks; <see cref="Loader.None"/>
/// skips loader checks.
/// </summary>
public sealed record CompatibilityContext(
    IReadOnlyList<InstalledContent> Items,
    GameVersion? ActiveVersion = null,
    Loader ActiveLoader = Loader.None);

/// <summary>
/// Pre-computed lookups built once per scan so each rule runs in O(n) rather than re-scanning the
/// library. Resolves a dependency identifier (project id / mod-id / slug) to the installed items
/// that provide it, and tracks which identifiers are required by anybody.
/// </summary>
public sealed class CompatibilityIndex
{
    private readonly Dictionary<string, List<InstalledContent>> _byIdentifier;
    private readonly HashSet<string> _requiredIdentifiers;

    public CompatibilityIndex(IReadOnlyList<InstalledContent> items)
    {
        _byIdentifier = new Dictionary<string, List<InstalledContent>>(StringComparer.OrdinalIgnoreCase);
        _requiredIdentifiers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (InstalledContent item in items)
        {
            foreach (string key in IdentifiersOf(item))
            {
                Add(key, item);
            }

            foreach (Dependency dep in item.Dependencies)
            {
                if (dep.Kind == DependencyKind.Required && !string.IsNullOrWhiteSpace(dep.Identifier))
                {
                    _requiredIdentifiers.Add(dep.Identifier);
                }
            }
        }
    }

    /// <summary>All installed items (enabled or not) that provide <paramref name="identifier"/>.</summary>
    public IReadOnlyList<InstalledContent> Resolve(string identifier)
        => _byIdentifier.TryGetValue(identifier, out List<InstalledContent>? list)
            ? list
            : [];

    /// <summary>True if some item declares a Required dependency on any identifier this item provides.</summary>
    public bool IsRequiredByAnyone(InstalledContent item)
        => IdentifiersOf(item).Any(id => _requiredIdentifiers.Contains(id));

    /// <summary>The id, project id and provided ids that can be used to refer to an item.</summary>
    public static IEnumerable<string> IdentifiersOf(InstalledContent item)
    {
        yield return item.Id;
        if (!string.IsNullOrWhiteSpace(item.ProjectId))
        {
            yield return item.ProjectId!;
        }

        foreach (string provided in item.ProvidedIds)
        {
            if (!string.IsNullOrWhiteSpace(provided))
            {
                yield return provided;
            }
        }
    }

    private void Add(string key, InstalledContent item)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!_byIdentifier.TryGetValue(key, out List<InstalledContent>? list))
        {
            list = [];
            _byIdentifier[key] = list;
        }

        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }
}

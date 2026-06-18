using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility.Rules;

/// <summary>Flags required dependencies that are not installed at all.</summary>
public sealed class MissingRequiredDependencyRule : ICompatibilityRule
{
    public IEnumerable<CompatibilityIssue> Evaluate(
        InstalledContent item,
        CompatibilityContext context,
        CompatibilityIndex index)
    {
        foreach (Dependency dep in item.Dependencies)
        {
            if (dep.Kind != DependencyKind.Required || string.IsNullOrWhiteSpace(dep.Identifier))
            {
                continue;
            }

            if (index.Resolve(dep.Identifier).Count == 0)
            {
                // If this item only knows the raw id (Modrinth gives no name for the dependency it
                // declares), borrow a human name another installed item declared for the same id -
                // purely from in-memory context, no network. Keeps the badge readable on older installs
                // until the next reconcile/update backfills the name onto this item too.
                string label = string.IsNullOrWhiteSpace(dep.DisplayName)
                    ? BorrowDisplayName(dep.Identifier, item, context) ?? dep.Label
                    : dep.Label;

                yield return CompatibilityIssue.Error(
                    CompatibilityKind.MissingDependency,
                    $"Requires {label}, which isn't installed.",
                    label);
            }
        }
    }

    /// <summary>
    /// Finds a human display name another installed item declares for <paramref name="identifier"/>,
    /// so a dependency known only by its raw id can still render readably. Offline only - it reads the
    /// scan context, never a mod source.
    /// </summary>
    private static string? BorrowDisplayName(string identifier, InstalledContent self, CompatibilityContext context)
    {
        foreach (InstalledContent other in context.Items)
        {
            if (ReferenceEquals(other, self))
            {
                continue;
            }

            foreach (Dependency dep in other.Dependencies)
            {
                if (!string.IsNullOrWhiteSpace(dep.DisplayName) &&
                    string.Equals(dep.Identifier, identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return dep.DisplayName;
                }
            }
        }

        return null;
    }
}

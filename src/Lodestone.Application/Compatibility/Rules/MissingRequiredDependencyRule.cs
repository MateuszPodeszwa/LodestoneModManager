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
                yield return CompatibilityIssue.Error(
                    CompatibilityKind.MissingDependency,
                    $"Requires {dep.Label}, which isn't installed.",
                    dep.Label);
            }
        }
    }
}

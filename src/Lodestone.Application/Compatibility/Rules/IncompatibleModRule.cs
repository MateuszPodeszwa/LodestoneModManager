using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility.Rules;

/// <summary>Flags an installed, enabled item that this item declares it is incompatible with.</summary>
public sealed class IncompatibleModRule : ICompatibilityRule
{
    public IEnumerable<CompatibilityIssue> Evaluate(
        InstalledContent item,
        CompatibilityContext context,
        CompatibilityIndex index)
    {
        foreach (Dependency dep in item.Dependencies)
        {
            if (dep.Kind != DependencyKind.Incompatible || string.IsNullOrWhiteSpace(dep.Identifier))
            {
                continue;
            }

            foreach (InstalledContent conflict in index.Resolve(dep.Identifier))
            {
                if (conflict.Enabled && !ReferenceEquals(conflict, item))
                {
                    yield return CompatibilityIssue.Error(
                        CompatibilityKind.Conflict,
                        $"Conflicts with {conflict.Name}, which is installed and enabled.",
                        conflict.Name);
                }
            }
        }
    }
}

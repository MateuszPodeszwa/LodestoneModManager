using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility.Rules;

/// <summary>Flags a required dependency that is installed but currently disabled (so it won't load).</summary>
public sealed class DisabledDependencyRule : ICompatibilityRule
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

            IReadOnlyList<InstalledContent> providers = index.Resolve(dep.Identifier);
            if (providers.Count > 0 && providers.All(p => !p.Enabled))
            {
                yield return CompatibilityIssue.Warning(
                    CompatibilityKind.DisabledDependency,
                    $"Requires {dep.Label}, which is installed but disabled.",
                    dep.Label);
            }
        }
    }
}

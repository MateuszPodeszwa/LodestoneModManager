using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility.Rules;

/// <summary>
/// Informational: a library (e.g. Fabric API) that no installed mod declares a dependency on. Not a
/// problem — just a hint that it may be safe to remove.
/// </summary>
public sealed class OrphanLibraryRule : ICompatibilityRule
{
    public IEnumerable<CompatibilityIssue> Evaluate(
        InstalledContent item,
        CompatibilityContext context,
        CompatibilityIndex index)
    {
        if (item.IsLibrary && !index.IsRequiredByAnyone(item))
        {
            yield return CompatibilityIssue.Info(
                CompatibilityKind.OrphanLibrary,
                $"No installed mod requires {item.Name}. You can keep it, but it may be unused.");
        }
    }
}

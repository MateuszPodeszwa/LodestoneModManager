using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility.Rules;

/// <summary>
/// Flags another enabled copy of the same project/mod installed alongside this one (e.g. installed
/// twice, or both a catalog and a dragged-in copy).
/// </summary>
public sealed class DuplicateRule : ICompatibilityRule
{
    public IEnumerable<CompatibilityIssue> Evaluate(
        InstalledContent item,
        CompatibilityContext context,
        CompatibilityIndex index)
    {
        foreach (InstalledContent other in context.Items)
        {
            if (ReferenceEquals(other, item) || !other.Enabled || other.Type != item.Type)
            {
                continue;
            }

            bool sameProject =
                !string.IsNullOrWhiteSpace(item.ProjectId) &&
                string.Equals(item.ProjectId, other.ProjectId, StringComparison.OrdinalIgnoreCase);

            bool sameName = string.Equals(item.Name, other.Name, StringComparison.OrdinalIgnoreCase);

            if (sameProject || sameName)
            {
                yield return CompatibilityIssue.Warning(
                    CompatibilityKind.Duplicate,
                    $"Another copy of {item.Name} is installed and enabled.",
                    other.Name);
                yield break; // one warning is enough
            }
        }
    }
}

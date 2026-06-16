using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility.Rules;

/// <summary>Flags items that declare supported versions but not the active profile version.</summary>
public sealed class GameVersionMismatchRule : ICompatibilityRule
{
    public IEnumerable<CompatibilityIssue> Evaluate(
        InstalledContent item,
        CompatibilityContext context,
        CompatibilityIndex index)
    {
        // No version selected (the "All versions" view) → nothing to check against.
        if (context.ActiveVersion is null)
        {
            yield break;
        }

        // No declared versions → unknown, not an error (see RISK-ANALYSIS §5).
        if (item.GameVersions.Count == 0)
        {
            yield break;
        }

        if (!item.SupportsVersion(context.ActiveVersion))
        {
            yield return CompatibilityIssue.Warning(
                CompatibilityKind.GameVersionMismatch,
                $"Not built for {context.ActiveVersion}. Supports {string.Join(", ", item.GameVersions)}.");
        }
    }
}

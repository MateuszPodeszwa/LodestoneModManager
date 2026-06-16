using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility;

/// <summary>
/// One link in the compatibility Chain-of-Responsibility. Each rule inspects a single item against
/// the scan context and yields zero or more issues for exactly the problem class it owns. Rules are
/// independent and individually unit-tested.
/// </summary>
public interface ICompatibilityRule
{
    IEnumerable<CompatibilityIssue> Evaluate(
        InstalledContent item,
        CompatibilityContext context,
        CompatibilityIndex index);
}

/// <summary>Runs the rule pipeline over a library and returns a report per item (keyed by id).</summary>
public interface ICompatibilityService
{
    IReadOnlyDictionary<string, CompatibilityReport> Analyze(CompatibilityContext context);
}

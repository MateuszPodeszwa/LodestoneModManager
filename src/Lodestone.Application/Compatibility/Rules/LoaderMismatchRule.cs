using Lodestone.Domain;
using Lodestone.Domain.Compatibility;

namespace Lodestone.Application.Compatibility.Rules;

/// <summary>Flags a mod whose loader differs from the active profile's loader.</summary>
public sealed class LoaderMismatchRule : ICompatibilityRule
{
    public IEnumerable<CompatibilityIssue> Evaluate(
        InstalledContent item,
        CompatibilityContext context,
        CompatibilityIndex index)
    {
        if (!item.Type.UsesLoader() || item.Loader == Loader.None || context.ActiveLoader == Loader.None)
        {
            yield break;
        }

        if (item.Loader != context.ActiveLoader)
        {
            yield return CompatibilityIssue.Warning(
                CompatibilityKind.LoaderMismatch,
                $"This is a {item.Loader.ToDisplayName()} mod, but the profile uses {context.ActiveLoader.ToDisplayName()}.");
        }
    }
}

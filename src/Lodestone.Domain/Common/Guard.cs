using System.Runtime.CompilerServices;

namespace Lodestone.Domain.Common;

/// <summary>Lightweight argument guards used to keep entity invariants honest.</summary>
public static class Guard
{
    public static string NotNullOrWhiteSpace(
        string? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("Value cannot be null or blank.", name)
            : value;
    }

    public static T NotNull<T>(
        T? value,
        [CallerArgumentExpression(nameof(value))] string? name = null)
        where T : class
    {
        return value ?? throw new ArgumentNullException(name);
    }
}

using System.Text.RegularExpressions;

namespace Lodestone.Application.Catalog;

/// <summary>
/// A tolerant comparer for the wildly inconsistent version strings mods use (<c>0.5.8</c>,
/// <c>19.5.0</c>, <c>r5.3</c>, <c>1.8.1+1.21</c>). It compares the leading numeric components; when
/// it genuinely cannot decide, it reports the values as different rather than equal, so an ambiguous
/// case surfaces as an available update the user can choose to take (never an unwanted auto-decision).
/// </summary>
public sealed partial class VersionComparer : IComparer<string>
{
    public static VersionComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (string.Equals(x, y, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        int[] left = ExtractNumbers(x);
        int[] right = ExtractNumbers(y);
        int max = Math.Max(left.Length, right.Length);
        for (int i = 0; i < max; i++)
        {
            int a = i < left.Length ? left[i] : 0;
            int b = i < right.Length ? right[i] : 0;
            if (a != b)
            {
                return a.CompareTo(b);
            }
        }

        // Numeric parts equal but strings differ → fall back to ordinal to keep a stable ordering.
        return string.CompareOrdinal(x ?? string.Empty, y ?? string.Empty);
    }

    /// <summary>True when <paramref name="candidate"/> should be treated as newer than <paramref name="current"/>.</summary>
    public static bool IsNewer(string? candidate, string? current)
        => Instance.Compare(candidate, current) > 0;

    /// <summary>
    /// Compares only the leading numeric components, treating equal numeric parts as equal (no ordinal
    /// tie-break, unlike <see cref="Compare"/>). Used for version-range satisfaction, where <c>1.0</c>
    /// and <c>1.0.0</c> must count as the same so an equality bound (<c>&gt;=</c>) isn't falsely failed.
    /// </summary>
    public static int CompareNumeric(string? x, string? y)
    {
        int[] left = ExtractNumbers(x);
        int[] right = ExtractNumbers(y);
        int max = Math.Max(left.Length, right.Length);
        for (int i = 0; i < max; i++)
        {
            int a = i < left.Length ? left[i] : 0;
            int b = i < right.Length ? right[i] : 0;
            if (a != b)
            {
                return a.CompareTo(b);
            }
        }

        return 0;
    }

    private static int[] ExtractNumbers(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        MatchCollection matches = NumberPattern().Matches(value);
        var numbers = new List<int>(matches.Count);
        foreach (Match m in matches)
        {
            if (int.TryParse(m.Value, out int n))
            {
                numbers.Add(n);
            }
        }

        return [.. numbers];
    }

    [GeneratedRegex(@"\d+", RegexOptions.CultureInvariant)]
    private static partial Regex NumberPattern();
}

using System.Text.RegularExpressions;

namespace Lodestone.Application.Common;

/// <summary>Helpers for turning file/display names into stable identifiers and tidy titles.</summary>
public static partial class Slug
{
    /// <summary>Lowercases and replaces runs of non-alphanumeric characters with single hyphens.</summary>
    public static string From(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "item";
        }

        string slug = NonAlphanumeric().Replace(input.Trim().ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "item" : slug;
    }

    /// <summary>Turns a bare file stem (<c>cool_mod-1.2</c>) into a display title (<c>cool mod 1.2</c>).</summary>
    public static string PrettifyFileName(string fileStem)
    {
        if (string.IsNullOrWhiteSpace(fileStem))
        {
            return "New file";
        }

        string cleaned = Separators().Replace(fileStem.Trim(), " ").Trim();
        return string.IsNullOrEmpty(cleaned) ? "New file" : cleaned;
    }

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonAlphanumeric();

    [GeneratedRegex("[-_]+", RegexOptions.CultureInvariant)]
    private static partial Regex Separators();
}

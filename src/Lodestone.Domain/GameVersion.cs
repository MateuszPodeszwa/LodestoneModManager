using System.Text.RegularExpressions;
using Lodestone.Domain.Common;

namespace Lodestone.Domain;

/// <summary>
/// A Minecraft version identifier (e.g. <c>1.21.4</c>). Release versions of the form
/// <c>MAJOR.MINOR[.PATCH]</c> are parsed into numeric components and compared component-wise;
/// anything else (snapshots like <c>24w44a</c>, pre-releases) is preserved verbatim and compared
/// ordinally. Equality is case-insensitive on the raw string.
/// </summary>
public sealed partial class GameVersion : IEquatable<GameVersion>, IComparable<GameVersion>
{
    private static readonly Regex ReleasePattern = BuildReleasePattern();
    private readonly int[] _parts;

    private GameVersion(string value, int[] parts, bool isRelease)
    {
        Value = value;
        _parts = parts;
        IsRelease = isRelease;
    }

    /// <summary>The canonical string, e.g. <c>1.21.4</c>.</summary>
    public string Value { get; }

    /// <summary>True when the version is a numeric release (not a snapshot/pre-release).</summary>
    public bool IsRelease { get; }

    /// <summary>Parses a version, returning a failure result for null/blank input.</summary>
    public static Result<GameVersion> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Failure<GameVersion>("game_version.empty", "Game version cannot be empty.");
        }

        string trimmed = raw.Trim();
        Match match = ReleasePattern.Match(trimmed);
        if (match.Success)
        {
            int[] parts = Array.ConvertAll(trimmed.Split('.'), int.Parse);
            return Result.Success(new GameVersion(trimmed, parts, isRelease: true));
        }

        return Result.Success(new GameVersion(trimmed, [], isRelease: false));
    }

    /// <summary>Convenience parser that throws on blank input; prefer <see cref="Create"/> at boundaries.</summary>
    public static GameVersion Parse(string raw)
    {
        Result<GameVersion> result = Create(raw);
        return result.IsSuccess
            ? result.Value
            : throw new FormatException(result.Error.Message);
    }

    public int CompareTo(GameVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        // Two numeric releases compare component-wise; a release always sorts above a snapshot;
        // two non-releases fall back to an ordinal comparison of their raw strings.
        if (IsRelease && other.IsRelease)
        {
            int max = Math.Max(_parts.Length, other._parts.Length);
            for (int i = 0; i < max; i++)
            {
                int a = i < _parts.Length ? _parts[i] : 0;
                int b = i < other._parts.Length ? other._parts[i] : 0;
                if (a != b)
                {
                    return a.CompareTo(b);
                }
            }

            return 0;
        }

        if (IsRelease != other.IsRelease)
        {
            return IsRelease ? 1 : -1;
        }

        return string.CompareOrdinal(Value, other.Value);
    }

    public bool Equals(GameVersion? other)
        => other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as GameVersion);

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public override string ToString() => Value;

    public static bool operator ==(GameVersion? left, GameVersion? right)
        => left is null ? right is null : left.Equals(right);

    public static bool operator !=(GameVersion? left, GameVersion? right) => !(left == right);

    public static bool operator <(GameVersion left, GameVersion right) => left.CompareTo(right) < 0;

    public static bool operator >(GameVersion left, GameVersion right) => left.CompareTo(right) > 0;

    public static bool operator <=(GameVersion left, GameVersion right) => left.CompareTo(right) <= 0;

    public static bool operator >=(GameVersion left, GameVersion right) => left.CompareTo(right) >= 0;

    [GeneratedRegex(@"^\d+(\.\d+){1,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex BuildReleasePattern();
}

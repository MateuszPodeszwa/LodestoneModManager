using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Lodestone.App.Converters;

/// <summary>True when the bound value equals the parameter; ConvertBack returns the parameter (for segmented selectors).</summary>
public sealed class EqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), parameter?.ToString(), StringComparison.OrdinalIgnoreCase);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? parameter ?? Binding.DoNothing : Binding.DoNothing;
}

/// <summary>Negates a boolean, so a binding can show UI when a flag is <c>false</c>.</summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
}

/// <summary>Maps <c>true</c> → <see cref="Visibility.Visible"/>, <c>false</c> → <see cref="Visibility.Collapsed"/>.</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Maps <c>true</c> → <see cref="Visibility.Collapsed"/>, <c>false</c> → <see cref="Visibility.Visible"/> (show when a flag is off).</summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Collapsed;
}

/// <summary>Maps a non-null value → <see cref="Visibility.Visible"/>, null → <see cref="Visibility.Collapsed"/>.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is not null ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Maps a positive count → <see cref="Visibility.Visible"/>, zero → <see cref="Visibility.Collapsed"/> (hides empty sections).</summary>
public sealed class CountToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is int n && n > 0 ? Visibility.Visible : Visibility.Collapsed;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>First letter of a name, uppercased, for the coloured avatar squares.</summary>
public sealed class FirstLetterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string s = value?.ToString() ?? "?";
        return string.IsNullOrEmpty(s) ? "?" : char.ToUpperInvariant(s[0]).ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Shows the profile selector's "all" option as "All versions".</summary>
public sealed class VersionLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Equals(value?.ToString(), "all", StringComparison.OrdinalIgnoreCase) ? "All versions" : value?.ToString() ?? string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Hashes a name to a stable colour from a fixed avatar palette.</summary>
public sealed class NameToColorBrushConverter : IValueConverter
{
    private static readonly Color[] Palette =
    [
        (Color)ColorConverter.ConvertFromString("#5ac26d"),
        (Color)ColorConverter.ConvertFromString("#5a91c2"),
        (Color)ColorConverter.ConvertFromString("#c25a8f"),
        (Color)ColorConverter.ConvertFromString("#c2a65a"),
        (Color)ColorConverter.ConvertFromString("#9a6cc9"),
        (Color)ColorConverter.ConvertFromString("#5ac2b4"),
        (Color)ColorConverter.ConvertFromString("#c27a5a"),
        (Color)ColorConverter.ConvertFromString("#7a9b4f"),
    ];

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string s = value?.ToString() ?? string.Empty;
        uint hash = 0;
        foreach (char c in s)
        {
            hash = (hash * 31) + c;
        }

        return new SolidColorBrush(Palette[hash % (uint)Palette.Length]);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
}

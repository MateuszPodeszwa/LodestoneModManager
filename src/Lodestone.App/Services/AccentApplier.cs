using System.Windows;
using System.Windows.Media;

namespace Lodestone.App.Services;

/// <summary>A selectable accent colour. The first entry is the free default; the rest are supporter perks.</summary>
public sealed record AccentOption(string Name, string Hex, bool SupporterOnly);

/// <summary>The curated accent palette. Adding one here makes it appear in Settings automatically.</summary>
public static class SupporterAccents
{
    public const string DefaultHex = "#FF5AC26D";

    public static IReadOnlyList<AccentOption> All { get; } =
    [
        new("Classic Green", DefaultHex, SupporterOnly: false),
        new("Amber", "#FFE3B341", SupporterOnly: true),
        new("Violet", "#FFB57BE0", SupporterOnly: true),
        new("Cyan", "#FF4FC4D6", SupporterOnly: true),
        new("Coral", "#FFE2719A", SupporterOnly: true),
    ];

    public static bool IsDefault(string? hex)
        => string.IsNullOrWhiteSpace(hex) || string.Equals(hex, DefaultHex, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Applies an accent colour at runtime by recolouring the shared accent brushes in
/// <c>Themes/Theme.xaml</c>. Those brushes aren't frozen, so mutating <see cref="SolidColorBrush.Color"/>
/// updates every <c>StaticResource</c> consumer live — no XAML changes needed. Non-supporters always get
/// the default; a stored custom accent is ignored unless the caller is a supporter.
/// </summary>
public static class AccentApplier
{
    public static void Apply(string? hex, bool isSupporter)
    {
        string effective = isSupporter && !SupporterAccents.IsDefault(hex) ? hex! : SupporterAccents.DefaultHex;
        Color accent = Parse(effective);

        // Fully qualified: bare "Application" would bind to the Lodestone.Application namespace here.
        ResourceDictionary? res = System.Windows.Application.Current?.Resources;
        if (res is null)
        {
            return;
        }

        SetBrush(res, "AccentBrush", accent);
        SetBrush(res, "AccentHoverBrush", Lighten(accent, 0.14));
        SetBrush(res, "AccentSoftBrush", WithAlpha(accent, 0x24));
        SetBrush(res, "AccentFaintBrush", WithAlpha(accent, 0x12));
        SetBrush(res, "AccentTextBrush", ContrastText(accent));
        if (res.Contains("AccentColor"))
        {
            res["AccentColor"] = accent;
        }
    }

    public static Color Parse(string hex)
    {
        try
        {
            return (Color)ColorConverter.ConvertFromString(hex)!;
        }
        catch (FormatException)
        {
            return (Color)ColorConverter.ConvertFromString(SupporterAccents.DefaultHex)!;
        }
    }

    private static void SetBrush(ResourceDictionary res, string key, Color color)
    {
        if (res[key] is SolidColorBrush brush && !brush.IsFrozen)
        {
            brush.Color = color; // shared instance → updates every StaticResource binding live
        }
        else
        {
            res[key] = new SolidColorBrush(color);
        }
    }

    private static Color Lighten(Color c, double amount) => Color.FromArgb(
        c.A,
        (byte)(c.R + (255 - c.R) * amount),
        (byte)(c.G + (255 - c.G) * amount),
        (byte)(c.B + (255 - c.B) * amount));

    private static Color WithAlpha(Color c, byte alpha) => Color.FromArgb(alpha, c.R, c.G, c.B);

    // Dark text on light accents, light text on dark ones (matches the prototype's accent-text role).
    private static Color ContrastText(Color c)
    {
        double luminance = (0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B);
        return luminance > 150 ? Color.FromRgb(0x10, 0x13, 0x0F) : Color.FromRgb(0xF2, 0xF2, 0xF4);
    }
}

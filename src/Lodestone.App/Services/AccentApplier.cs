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
/// Applies an accent colour at runtime. WPF freezes the brushes loaded from the <c>Source</c>'d
/// <c>Themes/Theme.xaml</c>, and a frozen <see cref="Freezable"/> can't be recoloured in place, so this
/// <b>replaces</b> the shared accent resources with fresh brushes. Consumers therefore reference the accent
/// roles with <c>{DynamicResource}</c>, which re-resolves when a resource entry changes - so switching the
/// accent recolours the whole UI live, with no restart. Non-supporters always get the default; a stored
/// custom accent is ignored unless the caller is a supporter.
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
        SetBrush(res, "AccentBorderBrush", WithAlpha(accent, 0x66));

        // Gradient accent roles are rebuilt and replaced (frozen brushes can't be recoloured in place). The
        // logo/heart tile is a light→dark accent diagonal; the supporter hero tints its top stop and fades
        // into the card colour. DynamicResource consumers pick up the replacement live.
        Color cardBg = res["CardBgColor"] is Color cb ? cb : Parse("#FF26262B");
        res["AccentTileBrush"] = MakeGradient(new Point(0, 0), new Point(1, 1),
            (Lighten(accent, 0.12), 0.0), (Darken(accent, 0.18), 1.0));
        res["AccentHeroBrush"] = MakeGradient(new Point(0, 0), new Point(0, 1),
            (WithAlpha(accent, 0x22), 0.0), (cardBg, 0.7));

        if (res.Contains("AccentColor"))
        {
            res["AccentColor"] = accent;
        }
    }

    /// <summary>The accent currently applied to the app resources (the default before any apply). Lets non-WPF
    /// surfaces such as the WebView2 description read the same accent the brushes use. Reads it back from
    /// <c>AccentBrush</c> - the one resource <see cref="Apply"/> always updates - rather than the
    /// <c>AccentColor</c> token, which is only refreshed when present at the top level.</summary>
    public static Color CurrentAccent()
    {
        ResourceDictionary? res = System.Windows.Application.Current?.Resources;
        return res?["AccentBrush"] is SolidColorBrush b ? b.Color : Parse(SupporterAccents.DefaultHex);
    }

    /// <summary>The applied accent as an opaque CSS hex (e.g. "#5AC26D"), for HTML/CSS surfaces.</summary>
    public static string CurrentAccentHex()
    {
        Color c = CurrentAccent();
        return $"#{c.R:X2}{c.G:X2}{c.B:X2}";
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
        // The XAML defaults arrive frozen, so the first apply replaces them; later applies can recolour the
        // unfrozen replacement in place. Either way DynamicResource consumers re-render.
        if (res[key] is SolidColorBrush brush && !brush.IsFrozen)
        {
            brush.Color = color;
        }
        else
        {
            res[key] = new SolidColorBrush(color);
        }
    }

    private static LinearGradientBrush MakeGradient(Point start, Point end, params (Color Color, double Offset)[] stops)
    {
        var brush = new LinearGradientBrush { StartPoint = start, EndPoint = end };
        foreach ((Color color, double offset) in stops)
        {
            brush.GradientStops.Add(new GradientStop(color, offset));
        }
        return brush;
    }

    private static Color Lighten(Color c, double amount) => Color.FromArgb(
        c.A,
        (byte)(c.R + (255 - c.R) * amount),
        (byte)(c.G + (255 - c.G) * amount),
        (byte)(c.B + (255 - c.B) * amount));

    private static Color Darken(Color c, double amount) => Color.FromArgb(
        c.A,
        (byte)(c.R * (1 - amount)),
        (byte)(c.G * (1 - amount)),
        (byte)(c.B * (1 - amount)));

    private static Color WithAlpha(Color c, byte alpha) => Color.FromArgb(alpha, c.R, c.G, c.B);

    // Dark text on light accents, light text on dark ones (the accent-text role).
    private static Color ContrastText(Color c)
    {
        double luminance = (0.299 * c.R) + (0.587 * c.G) + (0.114 * c.B);
        return luminance > 150 ? Color.FromRgb(0x10, 0x13, 0x0F) : Color.FromRgb(0xF2, 0xF2, 0xF4);
    }
}

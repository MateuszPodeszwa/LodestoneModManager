using System.Globalization;

namespace Lodestone.App.Services;

/// <summary>Display formatting helpers for counts and byte sizes.</summary>
public static class Format
{
    public static string Number(long n)
    {
        if (n >= 1_000_000)
        {
            return (n / 1_000_000.0).ToString(n >= 10_000_000 ? "0" : "0.0", CultureInfo.InvariantCulture) + "M";
        }

        if (n >= 1_000)
        {
            return (n / 1_000.0).ToString(n >= 10_000 ? "0" : "0.0", CultureInfo.InvariantCulture) + "K";
        }

        return n.ToString(CultureInfo.InvariantCulture);
    }

    public static string Size(double mb)
    {
        if (mb >= 1)
        {
            return (mb < 10 ? mb.ToString("0.0", CultureInfo.InvariantCulture) : Math.Round(mb).ToString(CultureInfo.InvariantCulture)) + " MB";
        }

        return Math.Round(mb * 1024).ToString(CultureInfo.InvariantCulture) + " KB";
    }
}

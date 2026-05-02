using System.IO;

namespace CopyTrail.Utilities;

public static class ColorUtilities
{
    // Palette of visually distinct hues used for generated app colors.
    private static readonly string[] GeneratedPalette =
    [
        "#E53E3E", "#DD6B20", "#D69E2E", "#38A169", "#319795",
        "#3182CE", "#805AD5", "#D53F8C", "#00B5D8", "#718096",
    ];

    // Produces a consistent accent hex color derived from an arbitrary seed string.
    public static string GenerateAccentHex(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
            return "#64748B";

        uint hash = 0;
        foreach (char c in seed)
            hash = hash * 31 + c;

        return GeneratedPalette[hash % (uint)GeneratedPalette.Length];
    }

    // Produces a very light tint suitable for card backgrounds from an accent hex.
    public static string GenerateSoftHex(string accentHex)
    {
        if (!TryParseHex(accentHex, out byte r, out byte g, out byte b))
            return "#F8FAFC";

        byte sr = Blend(r, 255, 0.9);
        byte sg = Blend(g, 255, 0.9);
        byte sb = Blend(b, 255, 0.9);
        return $"#{sr:X2}{sg:X2}{sb:X2}";
    }

    // Returns "#FFFFFF" or "#1E293B" — whichever is more readable over the given accent.
    public static string GetReadableForegroundHex(string accentHex)
    {
        if (!TryParseHex(accentHex, out byte r, out byte g, out byte b))
            return "#1E293B";

        double luminance = GetRelativeLuminance(r, g, b);
        return luminance > 0.35 ? "#1E293B" : "#FFFFFF";
    }

    // Attempts to derive a dominant single-color accent from a bitmap file.
    // Returns null on any failure so callers can fall back gracefully.
    public static string? TryGetDominantColorFromFile(string filePath)
    {
        try
        {
            if (!File.Exists(filePath)) return null;

            using var bitmap = new System.Drawing.Bitmap(filePath);
            int step = Math.Max(1, Math.Min(bitmap.Width, bitmap.Height) / 16);
            long rSum = 0, gSum = 0, bSum = 0, count = 0;

            for (int x = 0; x < bitmap.Width; x += step)
            {
                for (int y = 0; y < bitmap.Height; y += step)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.A < 128) continue; // skip transparent
                    rSum += pixel.R;
                    gSum += pixel.G;
                    bSum += pixel.B;
                    count++;
                }
            }

            if (count == 0) return null;

            byte avgR = (byte)(rSum / count);
            byte avgG = (byte)(gSum / count);
            byte avgB = (byte)(bSum / count);

            // If the average is near-white or near-gray, return null so the
            // caller falls back to the hash-generated palette.
            double saturation = GetSaturation(avgR, avgG, avgB);
            if (saturation < 0.15) return null;

            return $"#{avgR:X2}{avgG:X2}{avgB:X2}";
        }
        catch
        {
            return null;
        }
    }

    public static bool TryParseHex(string hex, out byte r, out byte g, out byte b)
    {
        r = g = b = 0;
        string h = hex.TrimStart('#');
        if (h.Length != 6) return false;
        try
        {
            r = Convert.ToByte(h[..2], 16);
            g = Convert.ToByte(h[2..4], 16);
            b = Convert.ToByte(h[4..6], 16);
            return true;
        }
        catch { return false; }
    }

    private static double GetRelativeLuminance(byte r, byte g, byte b)
    {
        double rL = Linearize(r / 255.0);
        double gL = Linearize(g / 255.0);
        double bL = Linearize(b / 255.0);
        return 0.2126 * rL + 0.7152 * gL + 0.0722 * bL;
    }

    private static double Linearize(double c) =>
        c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    private static double GetSaturation(byte r, byte g, byte b)
    {
        double max = Math.Max(r, Math.Max(g, b)) / 255.0;
        double min = Math.Min(r, Math.Min(g, b)) / 255.0;
        if (max == 0) return 0;
        return (max - min) / max;
    }

    private static byte Blend(byte value, byte target, double targetWeight) =>
        (byte)Math.Round(value * (1 - targetWeight) + target * targetWeight);
}

using System.Text.RegularExpressions;

namespace CopyTrail.Utilities;

public static class ColorValueParser
{
    private static readonly Regex HexColor =
        new(@"^#([0-9A-Fa-f]{3}|[0-9A-Fa-f]{6}|[0-9A-Fa-f]{8})$", RegexOptions.Compiled);

    private static readonly Regex RgbColor =
        new(@"^rgba?\(\s*\d{1,3}\s*,\s*\d{1,3}\s*,\s*\d{1,3}(\s*,\s*[\d.]+)?\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex HslColor =
        new(@"^hsla?\(\s*\d{1,3}\s*,\s*[\d.]+%\s*,\s*[\d.]+%(\s*,\s*[\d.]+)?\s*\)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static bool IsColorValue(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        return HexColor.IsMatch(trimmed) ||
               RgbColor.IsMatch(trimmed) ||
               HslColor.IsMatch(trimmed);
    }
}

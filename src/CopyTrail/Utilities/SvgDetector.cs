namespace CopyTrail.Utilities;

public static class SvgDetector
{
    public static bool IsSvg(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.TrimStart();
        return trimmed.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) &&
               trimmed.Contains("</svg>", StringComparison.OrdinalIgnoreCase);
    }
}

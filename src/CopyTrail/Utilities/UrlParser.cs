namespace CopyTrail.Utilities;

public static class UrlParser
{
    public static bool IsUrl(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        // Multi-line text is not a URL
        if (trimmed.Contains('\n') || trimmed.Contains('\r')) return false;
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
               (uri.Scheme == "http" || uri.Scheme == "https");
    }
}

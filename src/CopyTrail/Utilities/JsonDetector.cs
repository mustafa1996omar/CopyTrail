using System.Text.Json;

namespace CopyTrail.Utilities;

public static class JsonDetector
{
    public static bool IsJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();
        // Must start/end with object or array braces
        if (!(trimmed.StartsWith('{') && trimmed.EndsWith('}')) &&
            !(trimmed.StartsWith('[') && trimmed.EndsWith(']')))
            return false;
        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            return doc.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

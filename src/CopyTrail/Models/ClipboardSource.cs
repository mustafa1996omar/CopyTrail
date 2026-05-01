namespace CopyTrail.Models;

public sealed class ClipboardSource
{
    public string? SourceAppName { get; init; }
    public string? SourceProcessName { get; init; }
    public string? SourceProcessPath { get; init; }
    public string? SourceWindowTitle { get; init; }
    public ClipboardSourceKind SourceKind { get; init; } = ClipboardSourceKind.Unknown;
    public SourceVisualIdentity Identity { get; init; } = SourceVisualIdentity.Unknown;

    public static ClipboardSource Unknown { get; } = new();
}

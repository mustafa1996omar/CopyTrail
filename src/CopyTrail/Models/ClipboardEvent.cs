namespace CopyTrail.Models;

public sealed class ClipboardEvent
{
    public long Id { get; set; }
    public long ClipboardContentId { get; set; }

    public string? SourceAppName { get; set; }
    public string? SourceProcessName { get; set; }
    public string? SourceProcessPath { get; set; }
    public string? SourceWindowTitle { get; set; }
    public ClipboardSourceKind SourceKind { get; set; } = ClipboardSourceKind.Unknown;

    public DateTime CopiedAtUtc { get; set; } = DateTime.UtcNow;
    public int CopyCount { get; set; } = 1;
    public DateTime LastCopiedAtUtc { get; set; } = DateTime.UtcNow;
}

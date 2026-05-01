namespace CopyTrail.Models;

public sealed class ClipboardContent
{
    public long Id { get; set; }
    public string ContentHash { get; set; } = "";
    public ClipboardItemKind Kind { get; set; } = ClipboardItemKind.Unknown;

    public string? PlainText { get; set; }
    public string? HtmlText { get; set; }
    public string? RichText { get; set; }
    public string? MarkdownText { get; set; }
    public string? JsonText { get; set; }
    public string? SvgText { get; set; }
    public string? Url { get; set; }
    public string? ImagePath { get; set; }
    public string? ThumbnailPath { get; set; }

    // JSON-serialized list of file paths for FileReference kind
    public string? FileReferenceJson { get; set; }

    public string? PreviewText { get; set; }
    public long SizeBytes { get; set; }
    public bool IsLargeContent { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

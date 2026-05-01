namespace CopyTrail.Models;

public sealed class ClipPreview
{
    public string Title { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public string PreviewText { get; init; } = "";
    public string Badge { get; init; } = "";
    public string SourceLabel { get; init; } = "";
    public string TimeLabel { get; init; } = "";
    public string? ThumbnailPath { get; init; }
    public SourceVisualIdentity SourceIdentity { get; init; } = SourceVisualIdentity.Unknown;
    public int CopyCount { get; init; } = 1;
    public bool IsLargeContent { get; init; }
    public bool IsPinned { get; init; }
}

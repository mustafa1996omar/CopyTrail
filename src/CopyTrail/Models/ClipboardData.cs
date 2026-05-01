namespace CopyTrail.Models;

public readonly struct ClipboardData
{
    public bool HasImage { get; init; }
    public bool HasFileList { get; init; }
    public string? HtmlContent { get; init; }
    public string? PlainText { get; init; }
    public string? SourceProcessName { get; init; }
}

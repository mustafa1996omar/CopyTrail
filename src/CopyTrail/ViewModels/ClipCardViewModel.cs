using System.ComponentModel;
using System.Windows.Media;
using CopyTrail.Converters;
using CopyTrail.Data;
using CopyTrail.Models;
using CopyTrail.Utilities;

namespace CopyTrail.ViewModels;

public sealed class ClipCardViewModel : INotifyPropertyChanged
{
    private bool _isSelected;
    private bool _isPinned;

    public ClipboardContent? Content { get; init; }
    public SourceVisualIdentity Source { get; init; } = SourceVisualIdentity.Unknown;
    public ClipboardItemKind Kind { get; init; } = ClipboardItemKind.Unknown;
    public string ContentKind { get; init; } = "Text";
    public string Preview { get; init; } = "";
    public string CopiedAt { get; init; } = "";
    public string CopiedAtAbsolute { get; init; } = "";
    public int CopyCount { get; init; } = 1;
    public bool IsLargeContent { get; init; }
    public string? ThumbnailPath { get; init; }
    public bool IsImageCard { get; init; }
    public string? SourceWindowTitle { get; init; }
    public string? SourceProcessName { get; init; }

    public SolidColorBrush AccentBrush { get; init; } = new(Colors.SlateGray);
    public SolidColorBrush SoftAccentBrush { get; init; } = new(Colors.WhiteSmoke);
    public SolidColorBrush BadgeBackgroundBrush { get; init; } = new(Colors.LightGray);
    public SolidColorBrush BadgeForegroundBrush { get; init; } = new(Colors.DimGray);

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
    }

    public bool IsPinned
    {
        get => _isPinned;
        set { _isPinned = value; OnPropertyChanged(nameof(IsPinned)); }
    }

    public bool ShowCopyCount => CopyCount > 1;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string name) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static SolidColorBrush BrushFromHex(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(color);
    }

    public static ClipCardViewModel FromRecord(TimelineItemRecord record)
    {
        var content = record.Content;
        var evt = record.Event;

        var identity = AppNameMapper.Resolve(evt.SourceProcessName ?? "");
        var (badgeBg, badgeFg) = GetBadgeColors(content.Kind);
        string badgeLabel = GetBadgeLabel(content.Kind);

        bool isImage = content.Kind == ClipboardItemKind.Image ||
                       content.Kind == ClipboardItemKind.Screenshot;

        string preview = isImage
            ? BuildImagePreview(content)
            : content.PreviewText ?? content.PlainText ?? "";

        string copiedAt = RelativeTimeConverter.ToRelative(evt.LastCopiedAtUtc);
        string copiedAtAbsolute = evt.LastCopiedAtUtc.ToLocalTime().ToString("g");

        return new ClipCardViewModel
        {
            Content = content,
            Source = identity,
            Kind = content.Kind,
            ContentKind = badgeLabel,
            Preview = preview,
            CopiedAt = copiedAt,
            CopiedAtAbsolute = copiedAtAbsolute,
            CopyCount = evt.CopyCount,
            IsPinned = content.IsPinned,
            IsLargeContent = content.IsLargeContent,
            ThumbnailPath = content.ThumbnailPath,
            IsImageCard = isImage,
            SourceWindowTitle = evt.SourceWindowTitle,
            SourceProcessName = evt.SourceProcessName,
            AccentBrush = BrushFromHex(identity.AccentColorHex),
            SoftAccentBrush = BrushFromHex(identity.SoftAccentColorHex),
            BadgeBackgroundBrush = BrushFromHex(badgeBg),
            BadgeForegroundBrush = BrushFromHex(badgeFg),
        };
    }

    private static string BuildImagePreview(ClipboardContent content)
    {
        if (content.ThumbnailPath is not null)
            return "";
        return content.Kind == ClipboardItemKind.Screenshot ? "Screenshot" : "Image";
    }

    public static string GetBadgeLabel(ClipboardItemKind kind) => kind switch
    {
        ClipboardItemKind.Text => "Text",
        ClipboardItemKind.RichText => "Rich",
        ClipboardItemKind.Html => "HTML",
        ClipboardItemKind.Markdown => "MD",
        ClipboardItemKind.Url => "URL",
        ClipboardItemKind.Image => "Image",
        ClipboardItemKind.Screenshot => "Screenshot",
        ClipboardItemKind.Code => "Code",
        ClipboardItemKind.Json => "JSON",
        ClipboardItemKind.TerminalCommand => "$",
        ClipboardItemKind.ColorValue => "Color",
        ClipboardItemKind.Svg => "SVG",
        ClipboardItemKind.FileReference => "File",
        ClipboardItemKind.WordContent => "Word",
        ClipboardItemKind.PdfText => "PDF",
        _ => "?"
    };

    private static (string bg, string fg) GetBadgeColors(ClipboardItemKind kind) => kind switch
    {
        ClipboardItemKind.Text => ("#F3F4F6", "#374151"),
        ClipboardItemKind.RichText => ("#CCFBF1", "#0F766E"),
        ClipboardItemKind.Html => ("#FED7AA", "#C2410C"),
        ClipboardItemKind.Markdown => ("#EDE9FE", "#5B21B6"),
        ClipboardItemKind.Url => ("#DBEAFE", "#1E40AF"),
        ClipboardItemKind.Image => ("#E0E7FF", "#3730A3"),
        ClipboardItemKind.Screenshot => ("#F1F5F9", "#334155"),
        ClipboardItemKind.Code => ("#D1FAE5", "#065F46"),
        ClipboardItemKind.Json => ("#FEF3C7", "#92400E"),
        ClipboardItemKind.TerminalCommand => ("#F3F4F6", "#111827"),
        ClipboardItemKind.ColorValue => ("#F5F3FF", "#4C1D95"),
        ClipboardItemKind.Svg => ("#FCE7F3", "#9D174D"),
        ClipboardItemKind.FileReference => ("#F5F5F4", "#57534E"),
        ClipboardItemKind.WordContent => ("#EAF1FB", "#1A3666"),
        ClipboardItemKind.PdfText => ("#FEE2E2", "#991B1B"),
        _ => ("#F9FAFB", "#6B7280")
    };
}

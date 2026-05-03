using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
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
    public bool IsCodeCard { get; init; }
    public bool IsColorCard { get; init; }
    public bool IsUrlCard { get; init; }
    public SolidColorBrush? ColorSwatchBrush { get; init; }
    public string? UrlDomain { get; init; }
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

        var identity = App.IdentityService.GetIdentity(evt.SourceProcessName, evt.SourceProcessPath);
        var (badgeBg, badgeFg) = GetBadgeColors(content.Kind);
        string badgeLabel = GetBadgeLabel(content.Kind);

        bool isImage = content.Kind == ClipboardItemKind.Image ||
                       content.Kind == ClipboardItemKind.Screenshot;
        bool isCode = content.Kind == ClipboardItemKind.Code ||
                      content.Kind == ClipboardItemKind.Json ||
                      content.Kind == ClipboardItemKind.TerminalCommand;
        bool isColor = content.Kind == ClipboardItemKind.ColorValue;
        bool isUrl = content.Kind == ClipboardItemKind.Url;

        string preview = isImage
            ? BuildImagePreview(content)
            : BuildTextPreview(content);

        SolidColorBrush? colorSwatch = isColor ? BuildColorSwatch(content.PlainText) : null;

        string? urlDomain = null;
        if (isUrl && content.Url is not null &&
            Uri.TryCreate(content.Url, UriKind.Absolute, out var parsedUri))
        {
            urlDomain = parsedUri.Host;
        }

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
            IsCodeCard = isCode,
            IsColorCard = isColor,
            IsUrlCard = isUrl,
            ColorSwatchBrush = colorSwatch,
            UrlDomain = urlDomain,
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

    private static string BuildTextPreview(ClipboardContent content) => content.Kind switch
    {
        ClipboardItemKind.TerminalCommand => "$ " + (content.PlainText?.TrimEnd() ?? ""),
        ClipboardItemKind.Json => content.JsonText ?? content.PlainText ?? "",
        ClipboardItemKind.Url => content.Url ?? content.PlainText ?? "",
        ClipboardItemKind.FileReference => BuildFilePreview(content.FileReferenceJson),
        ClipboardItemKind.Svg => StripTags(content.SvgText ?? content.PlainText ?? ""),
        _ => content.PreviewText ?? content.PlainText ?? ""
    };

    private static string BuildFilePreview(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return "";
        try
        {
            var paths = JsonSerializer.Deserialize<List<string>>(json);
            if (paths is null || paths.Count == 0) return "";
            string first = System.IO.Path.GetFileName(paths[0]);
            return paths.Count == 1 ? first : $"{first} (+{paths.Count - 1} more)";
        }
        catch { return ""; }
    }

    private static string StripTags(string text) =>
        Regex.Replace(text, @"<[^>]+>", " ").Replace("  ", " ").Trim();

    private static SolidColorBrush BuildColorSwatch(string? colorValue)
    {
        if (!string.IsNullOrWhiteSpace(colorValue))
        {
            try
            {
                var color = (Color)ColorConverter.ConvertFromString(colorValue.Trim());
                return new SolidColorBrush(color);
            }
            catch { }
        }
        return new SolidColorBrush(Colors.Gray);
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
        ClipboardItemKind.Text => ("#26A1A1AA", "#D1D5DB"),
        ClipboardItemKind.RichText => ("#260F766E", "#5EEAD4"),
        ClipboardItemKind.Html => ("#26C2410C", "#FED7AA"),
        ClipboardItemKind.Markdown => ("#265B21B6", "#C4B5FD"),
        ClipboardItemKind.Url => ("#261E40AF", "#93C5FD"),
        ClipboardItemKind.Image => ("#263730A3", "#A5B4FC"),
        ClipboardItemKind.Screenshot => ("#26334155", "#94A3B8"),
        ClipboardItemKind.Code => ("#26065F46", "#6EE7B7"),
        ClipboardItemKind.Json => ("#2692400E", "#FCD34D"),
        ClipboardItemKind.TerminalCommand => ("#26111827", "#D1D5DB"),
        ClipboardItemKind.ColorValue => ("#264C1D95", "#DDD6FE"),
        ClipboardItemKind.Svg => ("#269D174D", "#FBCFE8"),
        ClipboardItemKind.FileReference => ("#2657534E", "#D6D3D1"),
        ClipboardItemKind.WordContent => ("#261A3666", "#BFDBFE"),
        ClipboardItemKind.PdfText => ("#26991B1B", "#FCA5A5"),
        _ => ("#26374151", "#9CA3AF")
    };
}

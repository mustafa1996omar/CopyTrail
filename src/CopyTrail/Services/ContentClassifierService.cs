using System.Text.RegularExpressions;
using CopyTrail.Models;
using CopyTrail.Utilities;

namespace CopyTrail.Services;

public sealed class ContentClassifierService
{
    private static readonly Regex StripTags =
        new(@"<[^>]*>", RegexOptions.Compiled);

    private static readonly Regex CollapseWhitespace =
        new(@"\s+", RegexOptions.Compiled);

    public ClipboardItemKind Classify(ClipboardData data)
    {
        if (data.HasImage)
            return IsSnippingTool(data.SourceProcessName)
                ? ClipboardItemKind.Screenshot
                : ClipboardItemKind.Image;

        if (data.HasFileList)
            return ClipboardItemKind.FileReference;

        if (!string.IsNullOrWhiteSpace(data.HtmlContent))
            return ClassifyHtml(data.HtmlContent, data.PlainText);

        if (!string.IsNullOrWhiteSpace(data.PlainText))
            return ClassifyPlainText(data.PlainText, data.SourceProcessName);

        return ClipboardItemKind.Unknown;
    }

    private static ClipboardItemKind ClassifyHtml(string html, string? plainText)
    {
        if (html.Contains("urn:schemas-microsoft-com:office:word", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("Microsoft Word", StringComparison.OrdinalIgnoreCase) ||
            html.Contains("mso-", StringComparison.OrdinalIgnoreCase))
            return ClipboardItemKind.WordContent;

        var text = string.IsNullOrWhiteSpace(plainText) ? StripHtmlTags(html) : plainText;

        if (UrlParser.IsUrl(text)) return ClipboardItemKind.Url;
        if (MarkdownDetector.IsMarkdown(text)) return ClipboardItemKind.Markdown;
        if (CodeSnippetDetector.IsCodeSnippet(text)) return ClipboardItemKind.Code;

        return ClipboardItemKind.Html;
    }

    private static ClipboardItemKind ClassifyPlainText(string text, string? sourceProcess)
    {
        if (IsPdfViewer(sourceProcess))
            return ClipboardItemKind.PdfText;

        if (UrlParser.IsUrl(text)) return ClipboardItemKind.Url;
        if (JsonDetector.IsJson(text)) return ClipboardItemKind.Json;
        if (ColorValueParser.IsColorValue(text)) return ClipboardItemKind.ColorValue;
        if (SvgDetector.IsSvg(text)) return ClipboardItemKind.Svg;
        if (TerminalCommandDetector.IsTerminalCommand(text)) return ClipboardItemKind.TerminalCommand;
        if (MarkdownDetector.IsMarkdown(text)) return ClipboardItemKind.Markdown;
        if (CodeSnippetDetector.IsCodeSnippet(text)) return ClipboardItemKind.Code;

        if (text.Contains('\r') || (text.Contains('\n') && text.Contains('\t')))
            return ClipboardItemKind.RichText;

        return ClipboardItemKind.Text;
    }

    private static bool IsSnippingTool(string? process) =>
        process is not null &&
        (process.Equals("SnippingTool", StringComparison.OrdinalIgnoreCase) ||
         process.Equals("SnippingTool.exe", StringComparison.OrdinalIgnoreCase) ||
         process.Equals("ScreenSketch", StringComparison.OrdinalIgnoreCase) ||
         process.Equals("ScreenSketch.exe", StringComparison.OrdinalIgnoreCase));

    private static bool IsPdfViewer(string? process) =>
        process is not null &&
        (process.Contains("Acrobat", StringComparison.OrdinalIgnoreCase) ||
         process.Contains("AcroRd32", StringComparison.OrdinalIgnoreCase) ||
         process.Contains("FoxitPDF", StringComparison.OrdinalIgnoreCase));

    private static string StripHtmlTags(string html)
    {
        var stripped = StripTags.Replace(html, " ");
        return CollapseWhitespace.Replace(stripped, " ").Trim();
    }
}

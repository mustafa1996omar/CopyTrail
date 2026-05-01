using System.Text.RegularExpressions;

namespace CopyTrail.Utilities;

public static class MarkdownDetector
{
    private static readonly Regex Heading =
        new(@"^#{1,6}\s+\S", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex BoldOrItalic =
        new(@"\*{1,2}\S.*?\S\*{1,2}", RegexOptions.Compiled);

    private static readonly Regex BulletListItem =
        new(@"^[-*+]\s+\S", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex NumberedListItem =
        new(@"^\d+\.\s+\S", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex CodeFence =
        new(@"^```", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex InlineLink =
        new(@"\[.+?\]\(.+?\)", RegexOptions.Compiled);

    private static readonly Regex Blockquote =
        new(@"^>\s+\S", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex TableRow =
        new(@"^\|.+\|$", RegexOptions.Multiline | RegexOptions.Compiled);

    public static bool IsMarkdown(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        int score = 0;
        if (Heading.IsMatch(text)) score++;
        if (BoldOrItalic.IsMatch(text)) score++;
        if (BulletListItem.IsMatch(text)) score++;
        if (NumberedListItem.IsMatch(text)) score++;
        if (CodeFence.IsMatch(text)) score += 2; // strong indicator
        if (InlineLink.IsMatch(text)) score++;
        if (Blockquote.IsMatch(text)) score++;
        if (TableRow.IsMatch(text)) score++;

        // Require at least 2 points to avoid classifying casual text as Markdown
        return score >= 2;
    }
}

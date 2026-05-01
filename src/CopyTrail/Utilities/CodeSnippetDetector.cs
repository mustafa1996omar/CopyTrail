using System.Text.RegularExpressions;

namespace CopyTrail.Utilities;

public static class CodeSnippetDetector
{
    // Keywords that are unambiguous programming tokens — not common English words.
    // "from" and "return" are excluded because they appear in everyday prose.
    private static readonly string[] CodeKeywords =
    [
        "function ", "class ", "public ", "private ", "protected ", "static ",
        "const ", "let ", "var ", "import ", "export ",
        "def ", "if __name__", "async ", "await ",
        "SELECT ", "INSERT ", "UPDATE ", "DELETE ", "FROM ", "WHERE ",
        "using ", "namespace ", "interface ", "enum ", "struct ",
        "void ", "#include", "fn ", "pub ", "impl ", "mod ",
        "<?php", "<?xml", "lambda ", "@Override", "package "
    ];

    private static readonly Regex IndentedBlock =
        new(@"^(\s{2,}|\t)", RegexOptions.Multiline | RegexOptions.Compiled);

    public static bool IsCodeSnippet(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var lines = text.Split('\n');

        foreach (var keyword in CodeKeywords)
        {
            if (text.Contains(keyword, StringComparison.Ordinal))
                return true;
        }

        // Multi-line with consistent indentation suggests code
        if (lines.Length >= 3 && IndentedBlock.IsMatch(text))
            return true;

        return false;
    }
}

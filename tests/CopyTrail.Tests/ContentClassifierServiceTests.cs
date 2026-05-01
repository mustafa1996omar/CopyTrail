using CopyTrail.Models;
using CopyTrail.Services;
using CopyTrail.Utilities;
using Xunit;

namespace CopyTrail.Tests;

public sealed class ContentClassifierServiceTests
{
    private readonly ContentClassifierService _classifier = new();

    // ── URL ──────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("https://www.google.com")]
    [InlineData("http://example.com/path?q=1")]
    [InlineData("https://github.com/user/repo")]
    public void Classify_HttpsUrl_ReturnsUrl(string url)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = url });
        Assert.Equal(ClipboardItemKind.Url, result);
    }

    [Theory]
    [InlineData("just some text")]
    [InlineData("not a url at all")]
    [InlineData("ftp://not-http.com")]
    public void Classify_NonUrl_DoesNotReturnUrl(string text)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = text });
        Assert.NotEqual(ClipboardItemKind.Url, result);
    }

    [Fact]
    public void Classify_MultilineTextWithUrl_DoesNotReturnUrl()
    {
        var text = "line one\nhttps://example.com\nline three";
        var result = _classifier.Classify(new ClipboardData { PlainText = text });
        Assert.NotEqual(ClipboardItemKind.Url, result);
    }

    // ── JSON ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("""{"key": "value"}""")]
    [InlineData("""{"name": "CopyTrail", "version": 1}""")]
    [InlineData("""[1, 2, 3]""")]
    [InlineData("""[{"id": 1}, {"id": 2}]""")]
    public void Classify_ValidJson_ReturnsJson(string json)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = json });
        Assert.Equal(ClipboardItemKind.Json, result);
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("42")]
    [InlineData("\"just a string\"")]
    [InlineData("{bad json}")]
    public void Classify_NonJson_DoesNotReturnJson(string text)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = text });
        Assert.NotEqual(ClipboardItemKind.Json, result);
    }

    // ── Color ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("#FF0000")]
    [InlineData("#abc")]
    [InlineData("#AABBCCDD")]
    [InlineData("rgb(255, 0, 0)")]
    [InlineData("rgba(255, 0, 0, 0.5)")]
    [InlineData("hsl(120, 100%, 50%)")]
    [InlineData("hsla(120, 100%, 50%, 0.3)")]
    public void Classify_ColorValue_ReturnsColorValue(string color)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = color });
        Assert.Equal(ClipboardItemKind.ColorValue, result);
    }

    [Theory]
    [InlineData("not a color")]
    [InlineData("#GGGGGG")]
    [InlineData("rgb(999, 999, 999, 999, 999)")]
    public void Classify_NonColor_DoesNotReturnColorValue(string text)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = text });
        Assert.NotEqual(ClipboardItemKind.ColorValue, result);
    }

    // ── SVG ──────────────────────────────────────────────────────────────────

    [Fact]
    public void Classify_SvgMarkup_ReturnsSvg()
    {
        var svg = """<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><circle cx="12" cy="12" r="10"/></svg>""";
        var result = _classifier.Classify(new ClipboardData { PlainText = svg });
        Assert.Equal(ClipboardItemKind.Svg, result);
    }

    [Fact]
    public void Classify_HtmlWithoutSvgClose_DoesNotReturnSvg()
    {
        var text = "<svg this is not complete";
        var result = _classifier.Classify(new ClipboardData { PlainText = text });
        Assert.NotEqual(ClipboardItemKind.Svg, result);
    }

    // ── Terminal command ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("git commit -m \"initial commit\"")]
    [InlineData("npm install --save-dev typescript")]
    [InlineData("dotnet build CopyTrail.sln")]
    [InlineData("docker run -it ubuntu bash")]
    [InlineData("$ ls -la")]
    [InlineData("kubectl get pods -n default")]
    public void Classify_TerminalCommand_ReturnsTerminalCommand(string cmd)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = cmd });
        Assert.Equal(ClipboardItemKind.TerminalCommand, result);
    }

    [Theory]
    [InlineData("The quick brown fox jumps over the lazy dog.")]
    [InlineData("Please review the attached document.")]
    public void Classify_NormalSentence_DoesNotReturnTerminalCommand(string text)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = text });
        Assert.NotEqual(ClipboardItemKind.TerminalCommand, result);
    }

    // ── Code ─────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("public class Foo { }")]
    [InlineData("function greet(name) {\n  return `Hello ${name}`;\n}")]
    [InlineData("def fibonacci(n):\n    if n <= 1:\n        return n\n    return fibonacci(n-1) + fibonacci(n-2)")]
    [InlineData("const x = 42;\nconst y = x * 2;")]
    [InlineData("SELECT id, name FROM users WHERE active = 1;")]
    [InlineData("import React from 'react';\nexport default function App() {}")]
    public void Classify_CodeSnippet_ReturnsCode(string code)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = code });
        Assert.Equal(ClipboardItemKind.Code, result);
    }

    // ── Markdown ─────────────────────────────────────────────────────────────

    [Fact]
    public void Classify_MarkdownWithHeadingAndList_ReturnsMarkdown()
    {
        var md = "# My Document\n\n- Item one\n- Item two\n- Item three";
        var result = _classifier.Classify(new ClipboardData { PlainText = md });
        Assert.Equal(ClipboardItemKind.Markdown, result);
    }

    [Fact]
    public void Classify_MarkdownWithCodeFence_ReturnsMarkdown()
    {
        var md = "Here is some code:\n\n```csharp\nvar x = 1;\n```\n\nEnd.";
        var result = _classifier.Classify(new ClipboardData { PlainText = md });
        Assert.Equal(ClipboardItemKind.Markdown, result);
    }

    [Fact]
    public void Classify_MarkdownWithLinkAndBold_ReturnsMarkdown()
    {
        var md = "**CopyTrail** is a [great app](https://example.com) for clipboard management.";
        var result = _classifier.Classify(new ClipboardData { PlainText = md });
        Assert.Equal(ClipboardItemKind.Markdown, result);
    }

    // ── Plain text fallback ───────────────────────────────────────────────────

    [Theory]
    [InlineData("Hello, world!")]
    [InlineData("This is just plain text with no special structure.")]
    [InlineData("Meeting at 3pm tomorrow.")]
    public void Classify_PlainText_ReturnsText(string text)
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = text });
        Assert.Equal(ClipboardItemKind.Text, result);
    }

    // ── Image and file ────────────────────────────────────────────────────────

    [Fact]
    public void Classify_ImageData_ReturnsImage()
    {
        var result = _classifier.Classify(new ClipboardData { HasImage = true });
        Assert.Equal(ClipboardItemKind.Image, result);
    }

    [Fact]
    public void Classify_ImageFromSnippingTool_ReturnsScreenshot()
    {
        var result = _classifier.Classify(new ClipboardData
        {
            HasImage = true,
            SourceProcessName = "SnippingTool"
        });
        Assert.Equal(ClipboardItemKind.Screenshot, result);
    }

    [Fact]
    public void Classify_FileList_ReturnsFileReference()
    {
        var result = _classifier.Classify(new ClipboardData { HasFileList = true });
        Assert.Equal(ClipboardItemKind.FileReference, result);
    }

    // ── HTML paths ────────────────────────────────────────────────────────────

    [Fact]
    public void Classify_WordHtml_ReturnsWordContent()
    {
        var html = "<html><body style=\"mso-margin-top-alt:auto\"><p>Word content</p></body></html>";
        var result = _classifier.Classify(new ClipboardData { HtmlContent = html });
        Assert.Equal(ClipboardItemKind.WordContent, result);
    }

    [Fact]
    public void Classify_GenericHtml_ReturnsHtml()
    {
        var html = "<div><p>Some content copied from a web page.</p></div>";
        var result = _classifier.Classify(new ClipboardData { HtmlContent = html });
        Assert.Equal(ClipboardItemKind.Html, result);
    }

    // ── Empty / null ──────────────────────────────────────────────────────────

    [Fact]
    public void Classify_EmptyData_ReturnsUnknown()
    {
        var result = _classifier.Classify(new ClipboardData());
        Assert.Equal(ClipboardItemKind.Unknown, result);
    }

    [Fact]
    public void Classify_WhitespaceOnly_ReturnsUnknown()
    {
        var result = _classifier.Classify(new ClipboardData { PlainText = "   " });
        Assert.Equal(ClipboardItemKind.Unknown, result);
    }
}

public sealed class UrlParserTests
{
    [Theory]
    [InlineData("https://www.example.com", true)]
    [InlineData("http://example.com/path", true)]
    [InlineData("ftp://example.com", false)]
    [InlineData("not a url", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("https://example.com\nhttps://other.com", false)]
    public void IsUrl_ReturnsExpected(string? input, bool expected)
    {
        Assert.Equal(expected, UrlParser.IsUrl(input));
    }
}

public sealed class JsonDetectorTests
{
    [Theory]
    [InlineData("""{"a":1}""", true)]
    [InlineData("""[1,2,3]""", true)]
    [InlineData("42", false)]
    [InlineData("\"string\"", false)]
    [InlineData("{bad}", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsJson_ReturnsExpected(string? input, bool expected)
    {
        Assert.Equal(expected, JsonDetector.IsJson(input));
    }
}

public sealed class ColorValueParserTests
{
    [Theory]
    [InlineData("#FFF", true)]
    [InlineData("#FFFFFF", true)]
    [InlineData("#FFFFFFFF", true)]
    [InlineData("rgb(0,0,0)", true)]
    [InlineData("rgba(0,0,0,1)", true)]
    [InlineData("hsl(0,0%,0%)", true)]
    [InlineData("hsla(0,0%,0%,1)", true)]
    [InlineData("red", false)]
    [InlineData("#GGGGGG", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsColorValue_ReturnsExpected(string? input, bool expected)
    {
        Assert.Equal(expected, ColorValueParser.IsColorValue(input));
    }
}

public sealed class SvgDetectorTests
{
    [Fact]
    public void IsSvg_ValidSvg_ReturnsTrue()
    {
        Assert.True(SvgDetector.IsSvg("<svg viewBox=\"0 0 10 10\"></svg>"));
    }

    [Fact]
    public void IsSvg_NoClosingTag_ReturnsFalse()
    {
        Assert.False(SvgDetector.IsSvg("<svg viewBox=\"0 0 10 10\">"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<div></div>")]
    public void IsSvg_NonSvg_ReturnsFalse(string? input)
    {
        Assert.False(SvgDetector.IsSvg(input));
    }
}

public sealed class TerminalCommandDetectorTests
{
    [Theory]
    [InlineData("git status", true)]
    [InlineData("npm run build", true)]
    [InlineData("$ echo hello", true)]
    [InlineData("dotnet restore", true)]
    [InlineData("docker ps -a", true)]
    [InlineData("The weather is nice today.", false)]
    [InlineData("Please send me the report.", false)]
    [InlineData(null, false)]
    public void IsTerminalCommand_ReturnsExpected(string? input, bool expected)
    {
        Assert.Equal(expected, TerminalCommandDetector.IsTerminalCommand(input));
    }
}

public sealed class MarkdownDetectorTests
{
    [Fact]
    public void IsMarkdown_HeadingPlusList_ReturnsTrue()
    {
        Assert.True(MarkdownDetector.IsMarkdown("# Title\n- item 1\n- item 2"));
    }

    [Fact]
    public void IsMarkdown_CodeFence_ReturnsTrue()
    {
        Assert.True(MarkdownDetector.IsMarkdown("Some text\n```\ncode\n```"));
    }

    [Fact]
    public void IsMarkdown_SingleIndicator_ReturnsFalse()
    {
        // A single "#" heading alone should not be enough
        Assert.False(MarkdownDetector.IsMarkdown("# Just a heading with nothing else here at all."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Plain sentence with no markdown.")]
    public void IsMarkdown_NonMarkdown_ReturnsFalse(string? input)
    {
        Assert.False(MarkdownDetector.IsMarkdown(input));
    }
}

public sealed class CodeSnippetDetectorTests
{
    [Theory]
    [InlineData("public class Foo { }", true)]
    [InlineData("function foo() {\n  return 1;\n}", true)]
    [InlineData("SELECT * FROM users;", true)]
    [InlineData("Hello, world!", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    public void IsCodeSnippet_ReturnsExpected(string? input, bool expected)
    {
        Assert.Equal(expected, CodeSnippetDetector.IsCodeSnippet(input));
    }
}

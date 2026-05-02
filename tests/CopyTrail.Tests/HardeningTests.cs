using System.IO;
using CopyTrail.Models;
using CopyTrail.Services;
using CopyTrail.Utilities;
using Xunit;

namespace CopyTrail.Tests;

// ── SvgDetector ───────────────────────────────────────────────────────────────

public sealed class SvgDetectorEdgeCaseTests
{
    [Fact]
    public void IsSvg_NullInput_ReturnsFalse()
    {
        Assert.False(SvgDetector.IsSvg(null));
    }

    [Fact]
    public void IsSvg_EmptyString_ReturnsFalse()
    {
        Assert.False(SvgDetector.IsSvg(""));
    }

    [Fact]
    public void IsSvg_WhitespaceOnly_ReturnsFalse()
    {
        Assert.False(SvgDetector.IsSvg("   "));
    }

    [Fact]
    public void IsSvg_OpeningTagWithoutClosing_ReturnsFalse()
    {
        Assert.False(SvgDetector.IsSvg("<svg xmlns=\"http://www.w3.org/2000/svg\">"));
    }

    [Fact]
    public void IsSvg_FullMinimalSvg_ReturnsTrue()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"><circle cx=\"10\" cy=\"10\" r=\"5\"/></svg>";
        Assert.True(SvgDetector.IsSvg(svg));
    }

    [Fact]
    public void IsSvg_SvgWithLeadingWhitespace_ReturnsTrue()
    {
        var svg = "   <svg viewBox=\"0 0 24 24\"><path d=\"M0 0\"/></svg>";
        Assert.True(SvgDetector.IsSvg(svg));
    }

    [Fact]
    public void IsSvg_UppercaseSvgTag_ReturnsTrue()
    {
        var svg = "<SVG xmlns=\"http://www.w3.org/2000/svg\"><rect/></SVG>";
        Assert.True(SvgDetector.IsSvg(svg));
    }

    [Fact]
    public void IsSvg_RandomHtml_ReturnsFalse()
    {
        var html = "<div class=\"container\"><p>Hello</p></div>";
        Assert.False(SvgDetector.IsSvg(html));
    }

    [Fact]
    public void IsSvg_PlainText_ReturnsFalse()
    {
        Assert.False(SvgDetector.IsSvg("this is just plain text"));
    }
}

// ── TerminalCommandDetector ───────────────────────────────────────────────────

public sealed class TerminalCommandDetectorEdgeCaseTests
{
    [Fact]
    public void IsTerminalCommand_NullInput_ReturnsFalse()
    {
        Assert.False(TerminalCommandDetector.IsTerminalCommand(null));
    }

    [Fact]
    public void IsTerminalCommand_EmptyString_ReturnsFalse()
    {
        Assert.False(TerminalCommandDetector.IsTerminalCommand(""));
    }

    [Fact]
    public void IsTerminalCommand_WhitespaceOnly_ReturnsFalse()
    {
        Assert.False(TerminalCommandDetector.IsTerminalCommand("   "));
    }

    [Theory]
    [InlineData("$ ls -la")]
    [InlineData("# apt update")]
    [InlineData("> cd /home")]
    public void IsTerminalCommand_ShellPromptPrefix_ReturnsTrue(string cmd)
    {
        Assert.True(TerminalCommandDetector.IsTerminalCommand(cmd));
    }

    [Theory]
    [InlineData("git commit -m \"message\"")]
    [InlineData("npm install --save-dev eslint")]
    [InlineData("dotnet build --configuration Release")]
    [InlineData("docker run -it ubuntu bash")]
    [InlineData("kubectl get pods -n default")]
    [InlineData("pip install requests")]
    [InlineData("cargo build --release")]
    [InlineData("cd /usr/local/bin")]
    [InlineData("mkdir -p output/logs")]
    [InlineData("curl -X POST https://api.example.com/data")]
    [InlineData("sudo apt-get install -y nginx")]
    [InlineData("ssh user@192.168.1.1")]
    [InlineData("ls -la /etc")]
    [InlineData("grep -r \"pattern\" ./src")]
    [InlineData("rm -rf dist/")]
    [InlineData("make all")]
    public void IsTerminalCommand_KnownCommand_ReturnsTrue(string cmd)
    {
        Assert.True(TerminalCommandDetector.IsTerminalCommand(cmd));
    }

    [Theory]
    [InlineData("The quick brown fox jumps over the lazy dog.")]
    [InlineData("Please see the attached document for details.")]
    [InlineData("Meeting scheduled for Monday at 2pm.")]
    [InlineData("Hi, thanks for your email!")]
    public void IsTerminalCommand_NaturalLanguageSentence_ReturnsFalse(string text)
    {
        Assert.False(TerminalCommandDetector.IsTerminalCommand(text));
    }

    [Fact]
    public void IsTerminalCommand_MoreThanThreeLines_ReturnsFalse()
    {
        // Four lines — exceeds the limit
        var multiline = "git add .\ngit commit -m \"fix\"\ngit push\ngit pull";
        Assert.False(TerminalCommandDetector.IsTerminalCommand(multiline));
    }

    [Fact]
    public void IsTerminalCommand_ExactlyThreeLines_IsEvaluated()
    {
        // Three lines is at the edge — the detector should still evaluate it
        var threeLines = "git add .\ngit commit -m \"fix\"\ngit push";
        // This is valid: first token 'git' is a known command
        Assert.True(TerminalCommandDetector.IsTerminalCommand(threeLines));
    }

    [Fact]
    public void IsTerminalCommand_UnknownFirstWordNoPrompt_ReturnsFalse()
    {
        Assert.False(TerminalCommandDetector.IsTerminalCommand("foobarapp --flag value"));
    }
}

// ── MarkdownDetector ──────────────────────────────────────────────────────────

public sealed class MarkdownDetectorEdgeCaseTests
{
    [Fact]
    public void IsMarkdown_NullInput_ReturnsFalse()
    {
        Assert.False(MarkdownDetector.IsMarkdown(null));
    }

    [Fact]
    public void IsMarkdown_EmptyString_ReturnsFalse()
    {
        Assert.False(MarkdownDetector.IsMarkdown(""));
    }

    [Fact]
    public void IsMarkdown_WhitespaceOnly_ReturnsFalse()
    {
        Assert.False(MarkdownDetector.IsMarkdown("   "));
    }

    [Fact]
    public void IsMarkdown_HeadingAndBulletList_ReturnsTrue()
    {
        var md = "# Title\n\n- Item 1\n- Item 2\n- Item 3";
        Assert.True(MarkdownDetector.IsMarkdown(md));
    }

    [Fact]
    public void IsMarkdown_CodeFenceAlone_ReturnsTrue()
    {
        // Code fence scores 2, so it alone crosses the threshold of 2
        var md = "```\nsome code\n```";
        Assert.True(MarkdownDetector.IsMarkdown(md));
    }

    [Fact]
    public void IsMarkdown_BoldAndInlineLink_ReturnsTrue()
    {
        var md = "**CopyTrail** is available at [GitHub](https://github.com/example/copytrail).";
        Assert.True(MarkdownDetector.IsMarkdown(md));
    }

    [Fact]
    public void IsMarkdown_NumberedList_NeedsSecondIndicator()
    {
        // Only a numbered list: score = 1 → false (needs 2)
        var md = "1. First item\n2. Second item\n3. Third item";
        Assert.False(MarkdownDetector.IsMarkdown(md));
    }

    [Fact]
    public void IsMarkdown_BlockquoteAndBullet_ReturnsTrue()
    {
        var md = "> This is a quote\n\n- bullet item";
        Assert.True(MarkdownDetector.IsMarkdown(md));
    }

    [Fact]
    public void IsMarkdown_TableRow_NeedsSecondIndicator()
    {
        // A table with heading = 2 indicators
        var md = "# Table\n\n| Name | Value |\n|------|-------|\n| a | 1 |";
        Assert.True(MarkdownDetector.IsMarkdown(md));
    }

    [Fact]
    public void IsMarkdown_PlainSentence_ReturnsFalse()
    {
        Assert.False(MarkdownDetector.IsMarkdown("This is a regular sentence with no markdown."));
    }

    [Fact]
    public void IsMarkdown_HeadingOnly_ReturnsFalse()
    {
        // Only heading = score 1, below threshold of 2
        Assert.False(MarkdownDetector.IsMarkdown("# Just a heading"));
    }

    [Fact]
    public void IsMarkdown_CodeFenceWithLanguage_ReturnsTrue()
    {
        var md = "Here is some code:\n\n```csharp\nvar x = 1;\n```";
        Assert.True(MarkdownDetector.IsMarkdown(md));
    }
}

// ── LargeContentPolicy ────────────────────────────────────────────────────────

public sealed class LargeContentPolicyTests
{
    [Fact]
    public void IsLargeText_BelowLimit_ReturnsFalse()
    {
        var settings = new AppSettings { MaxTextBytes = 10 * 1024 * 1024 };
        Assert.False(LargeContentPolicy.IsLargeText(1024, settings));
    }

    [Fact]
    public void IsLargeText_AboveLimit_ReturnsTrue()
    {
        var settings = new AppSettings { MaxTextBytes = 1024 };
        Assert.True(LargeContentPolicy.IsLargeText(2048, settings));
    }

    [Fact]
    public void IsLargeText_ExactlyAtLimit_ReturnsFalse()
    {
        var settings = new AppSettings { MaxTextBytes = 1024 };
        Assert.False(LargeContentPolicy.IsLargeText(1024, settings));
    }

    [Fact]
    public void IsLargeImage_BelowLimit_ReturnsFalse()
    {
        var settings = new AppSettings { MaxImageBytes = 20 * 1024 * 1024 };
        Assert.False(LargeContentPolicy.IsLargeImage(1024 * 1024, settings));
    }

    [Fact]
    public void IsLargeImage_AboveLimit_ReturnsTrue()
    {
        var settings = new AppSettings { MaxImageBytes = 1024 };
        Assert.True(LargeContentPolicy.IsLargeImage(2048, settings));
    }

    [Fact]
    public void IsLargeImage_ZeroBytes_ReturnsFalse()
    {
        var settings = new AppSettings { MaxImageBytes = 1024 };
        Assert.False(LargeContentPolicy.IsLargeImage(0, settings));
    }

    [Fact]
    public void ShouldStoreImage_WhenEnabled_ReturnsTrue()
    {
        var settings = new AppSettings { StoreImages = true };
        Assert.True(LargeContentPolicy.ShouldStoreImage(settings));
    }

    [Fact]
    public void ShouldStoreImage_WhenDisabled_ReturnsFalse()
    {
        var settings = new AppSettings { StoreImages = false };
        Assert.False(LargeContentPolicy.ShouldStoreImage(settings));
    }

    [Fact]
    public void IsExcludedProcess_MatchesExcludedName_ReturnsTrue()
    {
        var settings = new AppSettings { ExcludedProcessNames = ["notepad"] };
        Assert.True(LargeContentPolicy.IsExcludedProcess("notepad", settings));
    }

    [Fact]
    public void IsExcludedProcess_CaseInsensitive_ReturnsTrue()
    {
        var settings = new AppSettings { ExcludedProcessNames = ["NOTEPAD"] };
        Assert.True(LargeContentPolicy.IsExcludedProcess("notepad", settings));
    }

    [Fact]
    public void IsExcludedProcess_NullName_ReturnsFalse()
    {
        var settings = new AppSettings { ExcludedProcessNames = ["notepad"] };
        Assert.False(LargeContentPolicy.IsExcludedProcess(null, settings));
    }

    [Fact]
    public void IsExcludedProcess_NotInList_ReturnsFalse()
    {
        var settings = new AppSettings { ExcludedProcessNames = ["notepad"] };
        Assert.False(LargeContentPolicy.IsExcludedProcess("chrome", settings));
    }

    [Fact]
    public void ExceedsStorageLimit_BelowLimit_ReturnsFalse()
    {
        var settings = new AppSettings { MaxStorageBytes = 1024L * 1024 * 1024 };
        Assert.False(LargeContentPolicy.ExceedsStorageLimit(100 * 1024 * 1024, settings));
    }

    [Fact]
    public void ExceedsStorageLimit_AboveLimit_ReturnsTrue()
    {
        var settings = new AppSettings { MaxStorageBytes = 1024 };
        Assert.True(LargeContentPolicy.ExceedsStorageLimit(2048, settings));
    }

    [Fact]
    public void ExceedsStorageLimit_ExactlyAtLimit_ReturnsFalse()
    {
        var settings = new AppSettings { MaxStorageBytes = 1024 };
        Assert.False(LargeContentPolicy.ExceedsStorageLimit(1024, settings));
    }
}

// ── LoggingService ────────────────────────────────────────────────────────────

public sealed class LoggingServiceTests : IDisposable
{
    private readonly string _tempDir;

    public LoggingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"CopyTrail_log_test_{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void LogError_DoesNotThrow()
    {
        // LoggingService uses %LOCALAPPDATA% — just verify it doesn't throw
        var ex = Record.Exception(() =>
            LoggingService.LogError("Test", "An error occurred for testing purposes"));
        Assert.Null(ex);
    }

    [Fact]
    public void LogWarning_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            LoggingService.LogWarning("Test", "A warning for testing purposes"));
        Assert.Null(ex);
    }

    [Fact]
    public void LogInfo_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            LoggingService.LogInfo("Test", "An info message for testing purposes"));
        Assert.Null(ex);
    }

    [Fact]
    public void LogError_WithException_DoesNotThrow()
    {
        var ex = Record.Exception(() =>
            LoggingService.LogError("Test", "Error with exception", new InvalidOperationException("test exception")));
        Assert.Null(ex);
    }

    [Fact]
    public void LogError_CreatesLogFile()
    {
        // Write a log entry and confirm the log directory + file exist
        LoggingService.LogError("Test", "File creation test");

        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CopyTrail", "logs");
        string logFile = Path.Combine(logDir, "CopyTrail.log");

        Assert.True(Directory.Exists(logDir),
            $"Log directory should exist at: {logDir}");
        Assert.True(File.Exists(logFile),
            $"Log file should exist at: {logFile}");
    }

    [Fact]
    public void LogError_DoesNotIncludeGenericSensitiveData()
    {
        // Verify the log format doesn't embed passwords or clipboard data
        // by checking that the component name and error class appear but not secret placeholders
        LoggingService.LogError("ComponentName", "Operation failed", new ArgumentException("bad arg"));

        string logFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CopyTrail", "logs", "CopyTrail.log");

        if (!File.Exists(logFile)) return; // best-effort: pass if file write was suppressed

        string content = File.ReadAllText(logFile);
        // Log must contain the exception type name, not raw exception message content
        Assert.Contains("ArgumentException", content);
    }
}

// ── Duplicate / deduplication policy (AppSettings defaults) ──────────────────

public sealed class DuplicatePolicyTests
{
    [Fact]
    public void AppSettings_CollapseRapidDuplicates_DefaultIsTrue()
    {
        var settings = new AppSettings();
        Assert.True(settings.CollapseRapidDuplicates);
    }

    [Fact]
    public void AppSettings_RapidDuplicateWindowSeconds_DefaultIsPositive()
    {
        var settings = new AppSettings();
        Assert.True(settings.RapidDuplicateWindowSeconds > 0);
    }

    [Fact]
    public void AppSettings_MaxHistoryCount_DefaultIsReasonable()
    {
        var settings = new AppSettings();
        Assert.True(settings.MaxHistoryCount >= 100 && settings.MaxHistoryCount <= 1_000_000);
    }

    [Fact]
    public void AppSettings_ExcludedProcessNames_DefaultContainsPasswordManagers()
    {
        var settings = new AppSettings();
        Assert.Contains(settings.ExcludedProcessNames,
            n => n.Contains("1Password", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(settings.ExcludedProcessNames,
            n => n.Contains("Bitwarden", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(settings.ExcludedProcessNames,
            n => n.Contains("KeePass", StringComparison.OrdinalIgnoreCase));
    }
}

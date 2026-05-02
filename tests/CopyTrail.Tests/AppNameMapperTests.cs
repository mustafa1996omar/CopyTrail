using CopyTrail.Utilities;
using Xunit;

namespace CopyTrail.Tests;

public sealed class AppNameMapperTests
{
    [Theory]
    [InlineData("chrome",          "Google Chrome")]
    [InlineData("msedge",          "Microsoft Edge")]
    [InlineData("firefox",         "Firefox")]
    [InlineData("WINWORD",         "Microsoft Word")]
    [InlineData("OUTLOOK",         "Microsoft Outlook")]
    [InlineData("Code",            "VS Code")]
    [InlineData("Cursor",          "Cursor")]
    [InlineData("codex",           "Codex")]
    [InlineData("WindowsTerminal", "Windows Terminal")]
    [InlineData("powershell",      "PowerShell")]
    [InlineData("pwsh",            "PowerShell")]
    [InlineData("explorer",        "File Explorer")]
    [InlineData("SnippingTool",    "Screenshot")]
    [InlineData("mspaint",         "Paint")]
    public void Resolve_KnownProcess_ReturnsExpectedDisplayName(string processName, string expectedAppName)
    {
        var result = AppNameMapper.Resolve(processName);
        Assert.Equal(expectedAppName, result.AppName);
    }

    [Fact]
    public void Resolve_NullProcessName_ReturnsUnknown()
    {
        var result = AppNameMapper.Resolve(null);
        Assert.Equal("Unknown App", result.AppName);
    }

    [Fact]
    public void Resolve_EmptyProcessName_ReturnsUnknown()
    {
        var result = AppNameMapper.Resolve("");
        Assert.Equal("Unknown App", result.AppName);
    }

    [Fact]
    public void Resolve_WhitespaceProcessName_ReturnsUnknown()
    {
        var result = AppNameMapper.Resolve("   ");
        Assert.Equal("Unknown App", result.AppName);
    }

    [Fact]
    public void Resolve_UnknownProcess_UsesProcessNameAsDisplayName()
    {
        var result = AppNameMapper.Resolve("notepad");
        Assert.Equal("notepad", result.AppName);
    }

    [Fact]
    public void Resolve_UnknownProcess_UsesFirstCharAsInitial()
    {
        var result = AppNameMapper.Resolve("notepad");
        Assert.Equal("N", result.Initial);
    }

    [Fact]
    public void Resolve_UnknownProcess_HasDerivedAccentColor()
    {
        var resolved = AppNameMapper.Resolve("someunknownapp");
        // Unknown apps now get a hash-derived palette color instead of the gray fallback.
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", resolved.AccentColorHex);
        Assert.NotEqual("#64748B", resolved.AccentColorHex);
    }

    [Fact]
    public void Resolve_ProcessName_IsCaseInsensitiveForKnownApps()
    {
        var lower = AppNameMapper.Resolve("chrome");
        var upper = AppNameMapper.Resolve("CHROME");
        var mixed = AppNameMapper.Resolve("Chrome");
        Assert.Equal(lower.AppName, upper.AppName);
        Assert.Equal(lower.AppName, mixed.AppName);
    }

    [Theory]
    [InlineData("chrome")]
    [InlineData("msedge")]
    [InlineData("firefox")]
    [InlineData("WINWORD")]
    [InlineData("OUTLOOK")]
    [InlineData("Code")]
    [InlineData("Cursor")]
    [InlineData("codex")]
    [InlineData("WindowsTerminal")]
    [InlineData("powershell")]
    [InlineData("pwsh")]
    [InlineData("explorer")]
    [InlineData("SnippingTool")]
    [InlineData("mspaint")]
    public void IsKnown_KnownProcess_ReturnsTrue(string processName)
    {
        Assert.True(AppNameMapper.IsKnown(processName));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("notepad")]
    [InlineData("somerandombinary")]
    public void IsKnown_UnknownProcess_ReturnsFalse(string? processName)
    {
        Assert.False(AppNameMapper.IsKnown(processName));
    }

    [Theory]
    [InlineData("chrome",       "#4285F4")]
    [InlineData("msedge",       "#0078D7")]
    [InlineData("firefox",      "#FF7139")]
    [InlineData("WINWORD",      "#2B579A")]
    [InlineData("powershell",   "#012456")]
    [InlineData("pwsh",         "#012456")]
    public void Resolve_KnownProcess_HasExpectedAccentColor(string processName, string expectedHex)
    {
        var result = AppNameMapper.Resolve(processName);
        Assert.Equal(expectedHex, result.AccentColorHex);
    }

    [Fact]
    public void Resolve_KnownProcess_HasNonEmptyInitial()
    {
        var result = AppNameMapper.Resolve("chrome");
        Assert.False(string.IsNullOrEmpty(result.Initial));
    }
}

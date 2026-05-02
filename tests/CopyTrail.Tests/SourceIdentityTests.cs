using System.IO;
using CopyTrail.Services;
using CopyTrail.Utilities;
using Xunit;

namespace CopyTrail.Tests;

// Tests for ColorUtilities, AppIconService, and the identity fallback logic.
// No WPF types are used here — the test project does not have UseWPF.

public sealed class ColorUtilitiesTests
{
    // ── Known-app color mapping ──────────────────────────────────────────────

    [Theory]
    [InlineData("chrome",          "#4285F4")]
    [InlineData("msedge",          "#0078D7")]
    [InlineData("firefox",         "#FF7139")]
    [InlineData("WINWORD",         "#2B579A")]
    [InlineData("powershell",      "#012456")]
    [InlineData("pwsh",            "#012456")]
    public void KnownApp_HasExpectedAccentColor(string processName, string expectedHex)
    {
        var identity = AppNameMapper.Resolve(processName);
        Assert.Equal(expectedHex, identity.AccentColorHex);
    }

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
    [InlineData("explorer",        "File Explorer")]
    [InlineData("SnippingTool",    "Screenshot")]
    public void KnownApp_HasExpectedDisplayName(string processName, string expectedName)
    {
        var identity = AppNameMapper.Resolve(processName);
        Assert.Equal(expectedName, identity.AppName);
    }

    // ── Unknown app fallback ─────────────────────────────────────────────────

    [Fact]
    public void UnknownApp_HasNonGrayAccent()
    {
        var identity = AppNameMapper.Resolve("notepad");
        // Unknown apps now get a palette color, not the gray #64748B
        Assert.NotEqual("#64748B", identity.AccentColorHex);
    }

    [Fact]
    public void UnknownApp_AccentIsDeterministic()
    {
        var a = AppNameMapper.Resolve("notepad");
        var b = AppNameMapper.Resolve("notepad");
        Assert.Equal(a.AccentColorHex, b.AccentColorHex);
    }

    [Fact]
    public void UnknownApp_DifferentProcessNames_MayHaveDifferentColors()
    {
        var a = AppNameMapper.Resolve("notepad");
        var b = AppNameMapper.Resolve("msteams");
        // Very unlikely to collide; both should be valid hex colors.
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", a.AccentColorHex);
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", b.AccentColorHex);
    }

    [Fact]
    public void NullProcessName_ReturnsUnknownIdentity()
    {
        var identity = AppNameMapper.Resolve(null);
        Assert.Equal("Unknown App", identity.AppName);
    }

    // ── Soft color generation ─────────────────────────────────────────────────

    [Fact]
    public void GenerateSoftHex_ProducesLighterColor()
    {
        string accent = "#4285F4";
        string soft = ColorUtilities.GenerateSoftHex(accent);

        Assert.True(ColorUtilities.TryParseHex(soft, out byte sr, out byte sg, out byte sb));
        ColorUtilities.TryParseHex(accent, out byte ar, out byte ag, out byte ab);

        // Soft version should be lighter (higher average brightness)
        int softBrightness = sr + sg + sb;
        int accentBrightness = ar + ag + ab;
        Assert.True(softBrightness > accentBrightness,
            $"Expected soft ({soft}) to be lighter than accent ({accent})");
    }

    [Fact]
    public void GenerateSoftHex_ProducesValidHex()
    {
        string soft = ColorUtilities.GenerateSoftHex("#FF7139");
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", soft);
    }

    [Fact]
    public void GenerateSoftHex_InvalidInput_ReturnsFallback()
    {
        string soft = ColorUtilities.GenerateSoftHex("not-a-hex");
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", soft);
    }

    [Fact]
    public void GenerateAccentHex_NullOrEmpty_ReturnsFallback()
    {
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", ColorUtilities.GenerateAccentHex(""));
        Assert.Matches(@"^#[0-9A-Fa-f]{6}$", ColorUtilities.GenerateAccentHex("   "));
    }

    [Fact]
    public void GenerateAccentHex_IsDeterministic()
    {
        string a = ColorUtilities.GenerateAccentHex("someapp");
        string b = ColorUtilities.GenerateAccentHex("someapp");
        Assert.Equal(a, b);
    }

    [Fact]
    public void GenerateAccentHex_ProducesValidHex()
    {
        foreach (var name in new[] { "notepad", "mspaint", "winamp", "vlc", "steam" })
            Assert.Matches(@"^#[0-9A-Fa-f]{6}$", ColorUtilities.GenerateAccentHex(name));
    }

    // ── Readable foreground selection ─────────────────────────────────────────

    [Theory]
    [InlineData("#FFFFFF", "#1E293B")] // white background → dark text
    [InlineData("#F0F0F0", "#1E293B")] // very light → dark text
    [InlineData("#000000", "#FFFFFF")] // black background → white text
    [InlineData("#012456", "#FFFFFF")] // dark navy → white text
    public void GetReadableForeground_MatchesExpected(string accent, string expectedFg)
    {
        string fg = ColorUtilities.GetReadableForegroundHex(accent);
        Assert.Equal(expectedFg, fg);
    }

    [Fact]
    public void GetReadableForeground_InvalidHex_ReturnsDarkFallback()
    {
        string fg = ColorUtilities.GetReadableForegroundHex("not-valid");
        Assert.Equal("#1E293B", fg);
    }

    [Fact]
    public void GetReadableForeground_ReturnsOneOfTwoChoices()
    {
        string fg = ColorUtilities.GetReadableForegroundHex("#4285F4");
        Assert.True(fg == "#FFFFFF" || fg == "#1E293B");
    }

    // ── TryParseHex ───────────────────────────────────────────────────────────

    [Fact]
    public void TryParseHex_ValidHex_ReturnsTrue()
    {
        bool ok = ColorUtilities.TryParseHex("#4285F4", out byte r, out byte g, out byte b);
        Assert.True(ok);
        Assert.Equal(0x42, r);
        Assert.Equal(0x85, g);
        Assert.Equal(0xF4, b);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#123")]
    [InlineData("GGGGGG")]
    public void TryParseHex_Invalid_ReturnsFalse(string hex)
    {
        bool ok = ColorUtilities.TryParseHex(hex, out _, out _, out _);
        Assert.False(ok);
    }
}

public sealed class AppIconServiceTests
{
    private readonly AppIconService _service = new();

    // ── Safe icon cache filename generation ───────────────────────────────────

    [Fact]
    public void GetCacheFileName_WithProcessPath_ContainsNameAndHash()
    {
        string name = AppIconService.GetCacheFileName("notepad", @"C:\Windows\notepad.exe");
        Assert.StartsWith("notepad_", name);
        Assert.EndsWith(".png", name);
        // The hash portion is 8 hex chars
        Assert.Matches(@"^notepad_[0-9a-f]{8}\.png$", name);
    }

    [Fact]
    public void GetCacheFileName_WithoutProcessPath_ContainsNameOnly()
    {
        string name = AppIconService.GetCacheFileName("notepad", null);
        Assert.Equal("notepad.png", name);
    }

    [Fact]
    public void GetCacheFileName_SamePath_ProducesSameName()
    {
        string a = AppIconService.GetCacheFileName("chrome", @"C:\Program Files\Google\Chrome\Application\chrome.exe");
        string b = AppIconService.GetCacheFileName("chrome", @"C:\Program Files\Google\Chrome\Application\chrome.exe");
        Assert.Equal(a, b);
    }

    [Fact]
    public void GetCacheFileName_DifferentPaths_ProduceDifferentNames()
    {
        string a = AppIconService.GetCacheFileName("chrome", @"C:\path\a\chrome.exe");
        string b = AppIconService.GetCacheFileName("chrome", @"C:\path\b\chrome.exe");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetCacheFileName_PathIsCaseInsensitive()
    {
        // Same path, different case → same hash
        string a = AppIconService.GetCacheFileName("chrome", @"C:\CHROME\chrome.exe");
        string b = AppIconService.GetCacheFileName("chrome", @"C:\chrome\chrome.exe");
        Assert.Equal(a, b);
    }

    [Fact]
    public void GetCacheFileName_SpecialCharsInProcessName_AreSanitized()
    {
        string name = AppIconService.GetCacheFileName("my app!", null);
        Assert.DoesNotContain("!", name);
        Assert.DoesNotContain(" ", name);
        Assert.Matches(@"^[a-z0-9_\-\.]+$", name);
    }

    [Fact]
    public void GetCacheFileName_EmptyProcessName_UsesFallback()
    {
        string name = AppIconService.GetCacheFileName("", null);
        Assert.Equal("app.png", name);
    }

    [Fact]
    public void GetCachedIconPath_NonExistentFile_ReturnsNull()
    {
        string? result = _service.GetCachedIconPath("__nonexistent_app__", null);
        Assert.Null(result);
    }
}

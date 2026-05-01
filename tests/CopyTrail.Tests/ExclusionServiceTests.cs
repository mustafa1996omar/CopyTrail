using CopyTrail.Models;
using CopyTrail.Services;
using Xunit;

namespace CopyTrail.Tests;

public sealed class ExclusionServiceTests
{
    private static ExclusionService Create(IEnumerable<string>? exclusions = null)
    {
        var settings = new AppSettings
        {
            ExcludedProcessNames = [.. (exclusions ?? [])]
        };
        return new ExclusionService(settings);
    }

    // ── Empty list ────────────────────────────────────────────────────────────

    [Fact]
    public void IsExcluded_EmptyList_ReturnsFalse()
    {
        var svc = Create([]);
        Assert.False(svc.IsExcluded("notepad"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsExcluded_NullOrWhitespace_ReturnsFalse(string? processName)
    {
        var svc = Create(["notepad"]);
        Assert.False(svc.IsExcluded(processName));
    }

    // ── CopyTrail self-exclusion ───────────────────────────────────────────────

    [Theory]
    [InlineData("CopyTrail")]
    [InlineData("copytrail")]
    [InlineData("COPYTRAIL")]
    [InlineData("CopyTrail.exe")]
    [InlineData("copytrail.exe")]
    public void IsExcluded_CopyTrailSelf_AlwaysTrue(string processName)
    {
        var svc = Create([]);
        Assert.True(svc.IsExcluded(processName));
    }

    // ── Case-insensitive matching ─────────────────────────────────────────────

    [Theory]
    [InlineData("notepad", "notepad")]
    [InlineData("notepad", "Notepad")]
    [InlineData("notepad", "NOTEPAD")]
    [InlineData("Notepad", "notepad")]
    [InlineData("NOTEPAD", "notepad")]
    public void IsExcluded_CaseInsensitive_ReturnsTrue(string storedName, string processName)
    {
        var svc = Create([storedName]);
        Assert.True(svc.IsExcluded(processName));
    }

    // ── .exe suffix normalization ─────────────────────────────────────────────

    [Theory]
    [InlineData("1Password.exe", "1Password")]   // stored with .exe, runtime without
    [InlineData("1Password",     "1Password.exe")] // stored without .exe, runtime with
    [InlineData("KeePass.exe",   "KeePass")]
    [InlineData("KeePass.exe",   "keepass")]
    [InlineData("keepass.exe",   "KeePass")]
    public void IsExcluded_ExeSuffixNormalized_ReturnsTrue(string storedName, string processName)
    {
        var svc = Create([storedName]);
        Assert.True(svc.IsExcluded(processName));
    }

    // ── Default exclusion list ────────────────────────────────────────────────

    [Theory]
    [InlineData("1Password")]
    [InlineData("Bitwarden")]
    [InlineData("KeePass")]
    [InlineData("KeePassXC")]
    [InlineData("CredentialUIBroker")]
    public void IsExcluded_DefaultList_ExcludesPasswordManagers(string processName)
    {
        // Use real default settings — they store names with .exe
        var svc = new ExclusionService(new AppSettings());
        Assert.True(svc.IsExcluded(processName));
    }

    [Theory]
    [InlineData("1password.exe")]
    [InlineData("bitwarden.exe")]
    [InlineData("keepass.exe")]
    [InlineData("keepassxc.exe")]
    [InlineData("credentialuibroker.exe")]
    public void IsExcluded_DefaultListLowerCaseWithExe_ExcludesPasswordManagers(string processName)
    {
        var svc = new ExclusionService(new AppSettings());
        Assert.True(svc.IsExcluded(processName));
    }

    // ── Non-excluded apps ────────────────────────────────────────────────────

    [Theory]
    [InlineData("chrome")]
    [InlineData("notepad")]
    [InlineData("Code")]
    [InlineData("msedge")]
    public void IsExcluded_NonExcludedApp_ReturnsFalse(string processName)
    {
        var svc = new ExclusionService(new AppSettings());
        Assert.False(svc.IsExcluded(processName));
    }

    // ── Dynamic list modification ─────────────────────────────────────────────

    [Fact]
    public void IsExcluded_AfterAddingToList_ReturnsTrue()
    {
        var settings = new AppSettings { ExcludedProcessNames = [] };
        var svc = new ExclusionService(settings);

        Assert.False(svc.IsExcluded("notepad"));
        settings.ExcludedProcessNames.Add("notepad");
        Assert.True(svc.IsExcluded("notepad"));
    }

    [Fact]
    public void IsExcluded_AfterRemovingFromList_ReturnsFalse()
    {
        var settings = new AppSettings { ExcludedProcessNames = ["notepad"] };
        var svc = new ExclusionService(settings);

        Assert.True(svc.IsExcluded("notepad"));
        settings.ExcludedProcessNames.Remove("notepad");
        Assert.False(svc.IsExcluded("notepad"));
    }
}

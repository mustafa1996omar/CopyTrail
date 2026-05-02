using CopyTrail.Models;
using Xunit;

namespace CopyTrail.Tests;

public sealed class ThemeSystemTests
{
    [Fact]
    public void AppSettings_DefaultTheme_IsSystem()
    {
        var settings = new AppSettings();
        Assert.Equal(AppTheme.System, settings.Theme);
    }

    [Theory]
    [InlineData(AppTheme.Dark)]
    [InlineData(AppTheme.Light)]
    [InlineData(AppTheme.System)]
    public void AppTheme_AllValuesAreValid(AppTheme theme)
    {
        var settings = new AppSettings { Theme = theme };
        Assert.Equal(theme, settings.Theme);
    }
}

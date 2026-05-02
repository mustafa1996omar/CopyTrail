using System.Windows;
using CopyTrail.Models;
using Microsoft.Win32;

namespace CopyTrail.Services;

public static class ThemeService
{
    private const string RegistryKey =
        @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private const string RegistryValue = "AppsUseLightTheme";

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKey);
            if (key?.GetValue(RegistryValue) is int value)
                return value == 0;
        }
        catch { /* registry unavailable */ }
        return false;
    }

    public static void Apply(AppTheme theme)
    {
        bool dark = theme switch
        {
            AppTheme.Dark   => true,
            AppTheme.Light  => false,
            _               => IsSystemDark()
        };

        var uri = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };

        var merged = System.Windows.Application.Current.Resources.MergedDictionaries;
        if (merged.Count > 0)
            merged[0] = dict;
        else
            merged.Add(dict);
    }
}

using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using CopyTrail.Services;
using CopyTrail.ViewModels;
using WpfMessageBox = System.Windows.MessageBox;
using WpfMessageBoxButton = System.Windows.MessageBoxButton;
using WpfMessageBoxImage = System.Windows.MessageBoxImage;
using WpfMessageBoxResult = System.Windows.MessageBoxResult;

namespace CopyTrail.Views;

public partial class SettingsWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel(App.SettingsService.Current);
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseMaxHistory(out int maxHistory))
        {
            WpfMessageBox.Show(
                "Max history items must be a number between 100 and 100,000.",
                "CopyTrail",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Warning);
            return;
        }

        ViewModel.MaxHistoryCount = maxHistory;
        ViewModel.ApplyTo(App.SettingsService.Current);
        App.SettingsService.Save();
        StartupService.SetEnabled(App.SettingsService.Current.StartWithWindows);
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ResetDefaultsButton_Click(object sender, RoutedEventArgs e)
    {
        var result = WpfMessageBox.Show(
            "Reset all settings to their defaults?",
            "CopyTrail",
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Question,
            WpfMessageBoxResult.No);

        if (result != WpfMessageBoxResult.Yes) return;

        App.SettingsService.ResetToDefaults();
        StartupService.SetEnabled(App.SettingsService.Current.StartWithWindows);
        ViewModel.LoadFrom(App.SettingsService.Current);
    }

    private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
    {
        var result = WpfMessageBox.Show(
            "Clear all clipboard history? This cannot be undone.",
            "CopyTrail",
            WpfMessageBoxButton.YesNo,
            WpfMessageBoxImage.Warning,
            WpfMessageBoxResult.No);

        if (result != WpfMessageBoxResult.Yes) return;

        if (App.Repository is null)
        {
            WpfMessageBox.Show(
                "History could not be cleared — the database is not available.",
                "CopyTrail",
                WpfMessageBoxButton.OK,
                WpfMessageBoxImage.Warning);
            return;
        }

        _ = App.Repository.ClearAllAsync();
    }

    private void OpenDataFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var folder = App.SettingsService.DataFolderPath;
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = folder,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CopyTrail] Could not open data folder: {ex.Message}");
        }
    }

    private void MaxHistoryBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
    }

    private bool TryParseMaxHistory(out int value)
    {
        if (int.TryParse(MaxHistoryBox.Text, out value) && value >= 100 && value <= 100_000)
            return true;
        value = 0;
        return false;
    }

    private void AddExclusionButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NewExclusionBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        string normalized = name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name[..^4] : name;
        bool duplicate = ViewModel.ExcludedProcessNames.Any(
            existing => string.Equals(
                existing.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? existing[..^4] : existing,
                normalized, StringComparison.OrdinalIgnoreCase));

        if (!duplicate)
            ViewModel.ExcludedProcessNames.Add(name);

        NewExclusionBox.Clear();
    }

    private void RemoveExclusionButton_Click(object sender, RoutedEventArgs e)
    {
        if (ExclusionListBox.SelectedItem is string selected)
            ViewModel.ExcludedProcessNames.Remove(selected);
    }
}

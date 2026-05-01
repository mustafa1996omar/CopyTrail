using System.IO;
using System.Text.Json;
using CopyTrail.Models;

namespace CopyTrail.Services;

public sealed class SettingsService
{
    private static readonly string DataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "CopyTrail");

    private static readonly string SettingsFilePath = Path.Combine(DataFolder, "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Current { get; }

    public string DataFolderPath => DataFolder;

    public SettingsService()
    {
        Current = Load();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataFolder);
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(Current, JsonOptions));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CopyTrail] Settings save failed: {ex.Message}");
        }
    }

    public void ResetToDefaults()
    {
        var defaults = new AppSettings();
        Current.OpenPopupShortcut = defaults.OpenPopupShortcut;
        Current.StartWithWindows = defaults.StartWithWindows;
        Current.StoreImages = defaults.StoreImages;
        Current.MaxTextBytes = defaults.MaxTextBytes;
        Current.MaxImageBytes = defaults.MaxImageBytes;
        Current.CollapseRapidDuplicates = defaults.CollapseRapidDuplicates;
        Current.RapidDuplicateWindowSeconds = defaults.RapidDuplicateWindowSeconds;
        Current.MaxHistoryCount = defaults.MaxHistoryCount;
        Current.MaxStorageBytes = defaults.MaxStorageBytes;
        Save();
    }

    private static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsFilePath);
            return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CopyTrail] Settings load failed (using defaults): {ex.Message}");
            return new AppSettings();
        }
    }
}

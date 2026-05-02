using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CopyTrail.Models;

namespace CopyTrail.ViewModels;

public sealed class SettingsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _openPopupShortcut;
    private bool _startWithWindows;
    private bool _storeImages;
    private int _maxTextMb;
    private int _maxImageMb;
    private bool _collapseRapidDuplicates;
    private int _rapidDuplicateWindowSeconds;
    private int _maxHistoryCount;
    private int _maxStorageGb;

    public ObservableCollection<string> ExcludedProcessNames { get; } = [];

    public SettingsViewModel(AppSettings settings)
    {
        _openPopupShortcut = settings.OpenPopupShortcut;
        _startWithWindows = settings.StartWithWindows;
        _storeImages = settings.StoreImages;
        _maxTextMb = BytesToMb(settings.MaxTextBytes);
        _maxImageMb = BytesToMb(settings.MaxImageBytes);
        _collapseRapidDuplicates = settings.CollapseRapidDuplicates;
        _rapidDuplicateWindowSeconds = settings.RapidDuplicateWindowSeconds;
        _maxHistoryCount = settings.MaxHistoryCount;
        _maxStorageGb = BytesToGb(settings.MaxStorageBytes);
        foreach (var name in settings.ExcludedProcessNames)
            ExcludedProcessNames.Add(name);
    }

    public string OpenPopupShortcut
    {
        get => _openPopupShortcut;
        set { _openPopupShortcut = value; Notify(); }
    }

    public bool StartWithWindows
    {
        get => _startWithWindows;
        set { _startWithWindows = value; Notify(); }
    }

    public bool StoreImages
    {
        get => _storeImages;
        set { _storeImages = value; Notify(); }
    }

    public int MaxTextMb
    {
        get => _maxTextMb;
        set { _maxTextMb = value; Notify(); }
    }

    public int MaxImageMb
    {
        get => _maxImageMb;
        set { _maxImageMb = value; Notify(); }
    }

    public bool CollapseRapidDuplicates
    {
        get => _collapseRapidDuplicates;
        set { _collapseRapidDuplicates = value; Notify(); }
    }

    public int RapidDuplicateWindowSeconds
    {
        get => _rapidDuplicateWindowSeconds;
        set { _rapidDuplicateWindowSeconds = value; Notify(); }
    }

    public int MaxHistoryCount
    {
        get => _maxHistoryCount;
        set { _maxHistoryCount = value; Notify(); }
    }

    public int MaxStorageGb
    {
        get => _maxStorageGb;
        set { _maxStorageGb = value; Notify(); }
    }

    public void ApplyTo(AppSettings settings)
    {
        settings.OpenPopupShortcut = _openPopupShortcut;
        settings.StartWithWindows = _startWithWindows;
        settings.StoreImages = _storeImages;
        settings.MaxTextBytes = MbToBytes(_maxTextMb);
        settings.MaxImageBytes = MbToBytes(_maxImageMb);
        settings.CollapseRapidDuplicates = _collapseRapidDuplicates;
        settings.RapidDuplicateWindowSeconds = _rapidDuplicateWindowSeconds;
        settings.MaxHistoryCount = _maxHistoryCount;
        settings.MaxStorageBytes = GbToBytes(_maxStorageGb);
        settings.ExcludedProcessNames.Clear();
        foreach (var name in ExcludedProcessNames)
            settings.ExcludedProcessNames.Add(name);
    }

    public void LoadFrom(AppSettings settings)
    {
        OpenPopupShortcut = settings.OpenPopupShortcut;
        StartWithWindows = settings.StartWithWindows;
        StoreImages = settings.StoreImages;
        MaxTextMb = BytesToMb(settings.MaxTextBytes);
        MaxImageMb = BytesToMb(settings.MaxImageBytes);
        CollapseRapidDuplicates = settings.CollapseRapidDuplicates;
        RapidDuplicateWindowSeconds = settings.RapidDuplicateWindowSeconds;
        MaxHistoryCount = settings.MaxHistoryCount;
        MaxStorageGb = BytesToGb(settings.MaxStorageBytes);
        ExcludedProcessNames.Clear();
        foreach (var name in settings.ExcludedProcessNames)
            ExcludedProcessNames.Add(name);
    }

    private static int BytesToMb(long bytes) => (int)Math.Max(1, bytes / (1024 * 1024));
    private static long MbToBytes(int mb) => (long)mb * 1024 * 1024;
    private static int BytesToGb(long bytes) => (int)Math.Max(1, bytes / (1024L * 1024 * 1024));
    private static long GbToBytes(int gb) => (long)gb * 1024 * 1024 * 1024;

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

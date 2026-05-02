using System.IO;
using System.Threading;
using System.Windows;
using CopyTrail.Data;
using CopyTrail.Data.Repositories;
using CopyTrail.Models;
using CopyTrail.Services;
using CopyTrail.Views;

namespace CopyTrail;

public partial class App
{
    private const string MutexName = "CopyTrail_SingleInstance_Mutex";

    private Mutex? _mutex;
    private TrayService? _trayService;
    private HotkeyService? _hotkeyService;
    private ClipboardMonitorService? _clipboardMonitor;
    private PopupWindow? _popupWindow;
    private System.Threading.Timer? _pauseTimer;

    // Exposed for use by windows and services without full DI.
    public static SettingsService SettingsService { get; } = new();
    public static AppSettings Settings => SettingsService.Current;
    public static ClipboardRepository? Repository { get; private set; }
    public static ClipboardReaderService? ClipboardReader { get; private set; }
    public static PasteService? PasteService { get; private set; }
    public static CleanupService? CleanupService { get; private set; }
    public static FileStorageService? FileStorage { get; private set; }
    public static AppIconService IconService { get; } = new();
    public static SourceVisualIdentityService IdentityService { get; } = new(IconService);
    public static bool IsCaptureActive { get; private set; } = true;

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            _mutex.Dispose();
            Shutdown();
            return;
        }

        base.OnStartup(e);

        var dbContext = InitializeDatabase();

        if (dbContext is not null)
        {
            Repository = new ClipboardRepository(dbContext);

            var classifier = new ContentClassifierService();
            var sourceCapture = new SourceCaptureService();
            FileStorage = new FileStorageService(Settings);
            var exclusion = new ExclusionService(Settings);

            ClipboardReader = new ClipboardReaderService(
                Repository, classifier, sourceCapture, FileStorage, exclusion, Settings);

            CleanupService = new CleanupService(Repository, FileStorage);

            ClipboardReader.ItemStored += OnItemStored;

            _clipboardMonitor = new ClipboardMonitorService();
            _clipboardMonitor.ClipboardChanged += ClipboardReader.OnClipboardChanged;
            _clipboardMonitor.Initialize();

            PasteService = new PasteService(_clipboardMonitor);

            _ = Task.Run(() => CleanupService.RunStartupCleanupAsync());
        }
        else
        {
            PasteService = new PasteService(null);
        }

        StartupService.SetEnabled(Settings.StartWithWindows);

        _trayService = new TrayService();
        _trayService.OpenRequested += (_, _) => OpenOrFocusPopup();
        _trayService.SettingsRequested += (_, _) => OpenSettings();
        _trayService.ClearHistoryRequested += OnClearHistoryRequested;
        _trayService.CapturePauseRequested += OnCapturePauseRequested;
        _trayService.TimedPauseRequested += OnTimedPauseRequested;

        _hotkeyService = new HotkeyService();
        _hotkeyService.AltVPressed += (_, _) => TogglePopup();
        _hotkeyService.RegistrationFailed += (_, _) =>
            _trayService?.ShowBalloonTip(
                "CopyTrail",
                "Alt+V is already in use by another app. Change the hotkey in Settings.");
        _hotkeyService.Initialize();
    }

    private static CopyTrailDbContext? InitializeDatabase()
    {
        try
        {
            var dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CopyTrail");
            var dbPath = Path.Combine(dbFolder, "CopyTrail.db");
            var context = new CopyTrailDbContext(dbPath);
            DatabaseInitializer.Initialize(context);
            return context;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"CopyTrail could not initialize its database.\n\n{ex.Message}\n\nThe app will continue but history will not be saved.",
                "CopyTrail — Database Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return null;
        }
    }

    private void TogglePopup()
    {
        if (_popupWindow is { IsVisible: true })
        {
            if (_popupWindow.IsActive)
                _popupWindow.Close();
            else
                _popupWindow.Activate();
            return;
        }
        OpenPopup();
    }

    private void OpenOrFocusPopup()
    {
        if (_popupWindow is { IsVisible: true })
        {
            _popupWindow.Activate();
            return;
        }
        OpenPopup();
    }

    private void OpenPopup()
    {
        _popupWindow ??= CreatePopupWindow();
        _popupWindow.Show();
        _popupWindow.Activate();
    }

    private PopupWindow CreatePopupWindow()
    {
        var previousWindow = _hotkeyService?.LastForegroundWindow ?? IntPtr.Zero;
        var w = new PopupWindow(previousWindow);
        w.Closed += (_, _) => _popupWindow = null;
        return w;
    }

    public static void OpenSettings()
    {
        var win = new SettingsWindow();
        win.ShowDialog();
    }

    private void OnItemStored(object? sender, EventArgs e)
    {
        if (CleanupService is not null)
            _ = Task.Run(() => CleanupService.RunAfterCaptureAsync(Settings));
    }

    private async void OnClearHistoryRequested(object? sender, EventArgs e)
    {
        var result = System.Windows.MessageBox.Show(
            "Clear clipboard history? Pinned items will be kept.",
            "CopyTrail",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning,
            System.Windows.MessageBoxResult.No);

        if (result != System.Windows.MessageBoxResult.Yes) return;
        if (Repository is null) return;

        var imagePaths = await Repository.GetUnpinnedImagePathsAsync();
        await Repository.ClearAllAsync(keepPinned: true);

        if (FileStorage is not null)
        {
            foreach (var path in imagePaths)
                FileStorage.DeleteMediaFileIfExists(path);
        }

        _popupWindow?.RefreshAsync();
    }

    private void OnCapturePauseRequested(object? sender, EventArgs e)
    {
        _pauseTimer?.Dispose();
        _pauseTimer = null;
        IsCaptureActive = !IsCaptureActive;
        if (ClipboardReader is not null)
            ClipboardReader.IsPaused = !IsCaptureActive;
        _trayService?.SetCapturePaused(!IsCaptureActive);
    }

    private void OnTimedPauseRequested(object? sender, TimeSpan duration)
    {
        // Pause immediately.
        _pauseTimer?.Dispose();
        _pauseTimer = null;
        IsCaptureActive = false;
        if (ClipboardReader is not null)
            ClipboardReader.IsPaused = true;
        _trayService?.SetCapturePaused(true);

        // Schedule auto-resume on the UI thread.
        _pauseTimer = new System.Threading.Timer(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                _pauseTimer?.Dispose();
                _pauseTimer = null;
                IsCaptureActive = true;
                if (ClipboardReader is not null)
                    ClipboardReader.IsPaused = false;
                _trayService?.SetCapturePaused(false);
            });
        }, null, (long)duration.TotalMilliseconds, Timeout.Infinite);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _pauseTimer?.Dispose();
        _popupWindow?.Close();
        _clipboardMonitor?.Dispose();
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

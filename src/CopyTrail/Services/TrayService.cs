using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace CopyTrail.Services;

internal sealed class TrayService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _pauseItem;
    private readonly ToolStripMenuItem _resumeItem;

    public event EventHandler? OpenRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ClearHistoryRequested;
    public event EventHandler? CapturePauseRequested;
    public event EventHandler<TimeSpan>? TimedPauseRequested;

    public TrayService()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "CopyTrail — Capture active",
            Visible = true,
            Icon = LoadAppIcon()
        };

        var pauseUntilItem = new ToolStripMenuItem("Until resumed", null, OnPauseCaptureClicked);
        var pause5MinItem  = new ToolStripMenuItem("For 5 minutes", null, OnPause5MinClicked);
        var pause1HourItem = new ToolStripMenuItem("For 1 hour",    null, OnPause1HourClicked);

        _pauseItem = new ToolStripMenuItem("Pause Capture");
        _pauseItem.DropDownItems.Add(pauseUntilItem);
        _pauseItem.DropDownItems.Add(pause5MinItem);
        _pauseItem.DropDownItems.Add(pause1HourItem);

        _resumeItem = new ToolStripMenuItem("Resume Capture", null, OnResumeCaptureClicked) { Visible = false };

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("Open CopyTrail", null, OnOpenClicked);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(_pauseItem);
        contextMenu.Items.Add(_resumeItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Settings", null, OnSettingsClicked);
        contextMenu.Items.Add("Clear History...", null, OnClearHistoryClicked);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("Exit", null, OnExitClicked);
        _notifyIcon.ContextMenuStrip = contextMenu;

        _notifyIcon.DoubleClick += OnOpenClicked;
    }

    public void SetCapturePaused(bool paused)
    {
        _pauseItem.Visible = !paused;
        _resumeItem.Visible = paused;
        _notifyIcon.Text = paused
            ? "CopyTrail — Capture paused"
            : "CopyTrail — Capture active";
    }

    private static Icon LoadAppIcon()
    {
        var asm = System.Reflection.Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("CopyTrail.Resources.Icons.AppIcon.png");
        if (stream is null)
            return CreatePlaceholderIcon();
        using var bmp = new Bitmap(stream);
        var handle = bmp.GetHicon();
        return Icon.FromHandle(handle);
    }

    private static Icon CreatePlaceholderIcon()
    {
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.FillRectangle(new SolidBrush(System.Drawing.Color.FromArgb(0x25, 0x63, 0xEB)), 0, 0, 16, 16);
        var handle = bitmap.GetHicon();
        return Icon.FromHandle(handle);
    }

    private void OnOpenClicked(object? sender, EventArgs e) =>
        OpenRequested?.Invoke(this, EventArgs.Empty);

    private void OnSettingsClicked(object? sender, EventArgs e) =>
        SettingsRequested?.Invoke(this, EventArgs.Empty);

    private void OnClearHistoryClicked(object? sender, EventArgs e) =>
        ClearHistoryRequested?.Invoke(this, EventArgs.Empty);

    private void OnPauseCaptureClicked(object? sender, EventArgs e) =>
        CapturePauseRequested?.Invoke(this, EventArgs.Empty);

    private void OnResumeCaptureClicked(object? sender, EventArgs e) =>
        CapturePauseRequested?.Invoke(this, EventArgs.Empty);

    private void OnPause5MinClicked(object? sender, EventArgs e) =>
        TimedPauseRequested?.Invoke(this, TimeSpan.FromMinutes(5));

    private void OnPause1HourClicked(object? sender, EventArgs e) =>
        TimedPauseRequested?.Invoke(this, TimeSpan.FromHours(1));

    private static void OnExitClicked(object? sender, EventArgs e) =>
        System.Windows.Application.Current.Shutdown();

    public void ShowBalloonTip(string title, string message)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}

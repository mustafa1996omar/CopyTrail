using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using CopyTrail.Helpers;

namespace CopyTrail.Services;

public sealed class ClipboardMonitorService : IDisposable
{
    private HwndSource? _hwndSource;
    private volatile bool _suppressNextChange;

    public event EventHandler? ClipboardChanged;

    public void Initialize()
    {
        var parameters = new HwndSourceParameters("CopyTrail_ClipboardMonitorHost")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        bool ok = Win32Helpers.AddClipboardFormatListener(_hwndSource.Handle);
        if (!ok)
        {
            int err = Marshal.GetLastWin32Error();
            Debug.WriteLine($"[CopyTrail] AddClipboardFormatListener failed. Error: {err}. Running in degraded mode.");
        }
        else
        {
            Debug.WriteLine("[CopyTrail] Clipboard listener registered.");
        }
    }

    /// <summary>
    /// Call before CopyTrail writes to the clipboard to suppress the resulting
    /// WM_CLIPBOARDUPDATE from being forwarded as a user clipboard change.
    /// </summary>
    public void SuppressNextChange() => _suppressNextChange = true;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Helpers.WM_CLIPBOARDUPDATE)
        {
            if (_suppressNextChange)
            {
                _suppressNextChange = false;
                Debug.WriteLine("[CopyTrail] Suppressed self-triggered clipboard change.");
            }
            else
            {
                Debug.WriteLine("[CopyTrail] WM_CLIPBOARDUPDATE received.");
                ClipboardChanged?.Invoke(this, EventArgs.Empty);
            }
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_hwndSource is not null)
        {
            Win32Helpers.RemoveClipboardFormatListener(_hwndSource.Handle);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}

using System.Diagnostics;
using System.Windows.Interop;
using CopyTrail.Helpers;

namespace CopyTrail.Services;

internal sealed class HotkeyService : IDisposable
{
    private const int HotkeyId = 9001;

    private HwndSource? _hwndSource;

    public event EventHandler? AltVPressed;
    public event EventHandler? RegistrationFailed;

    public IntPtr LastForegroundWindow { get; private set; }

    public void Initialize()
    {
        var parameters = new HwndSourceParameters("CopyTrail_HotkeyHost")
        {
            Width = 0,
            Height = 0,
            WindowStyle = 0
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        bool registered = Win32Helpers.RegisterHotKey(
            _hwndSource.Handle,
            HotkeyId,
            Win32Helpers.MOD_ALT,
            Win32Helpers.VK_V);

        if (!registered)
        {
            int error = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
            Debug.WriteLine($"[CopyTrail] Failed to register Alt+V hotkey. Win32 error: {error}. Another app may be using it.");
            LoggingService.LogWarning("HotkeyService", $"Failed to register Alt+V hotkey. Win32 error: {error}. Another app may be using it.");
            RegistrationFailed?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Debug.WriteLine("[CopyTrail] Alt+V hotkey registered successfully.");
            LoggingService.LogInfo("HotkeyService", "Alt+V hotkey registered successfully.");
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Helpers.WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            LastForegroundWindow = Win32Helpers.GetForegroundWindow();
            Debug.WriteLine("[CopyTrail] Alt+V pressed.");
            AltVPressed?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_hwndSource is not null)
        {
            Win32Helpers.UnregisterHotKey(_hwndSource.Handle, HotkeyId);
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }
}

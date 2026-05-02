using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;
using CopyTrail.Helpers;
using CopyTrail.Models;
using WpfClipboard = System.Windows.Clipboard;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDataObject = System.Windows.DataObject;

namespace CopyTrail.Services;

public sealed class PasteService
{
    private readonly ClipboardMonitorService? _monitor;

    public PasteService(ClipboardMonitorService? monitor)
    {
        _monitor = monitor;
    }

    /// <summary>
    /// Writes content to clipboard, closes popup, restores focus to the target window,
    /// then simulates Ctrl+V. Returns false if the clipboard write fails (popup stays open).
    /// </summary>
    public async Task<bool> PasteAsync(ClipboardContent content, IntPtr targetWindow, Action closePopup)
    {
        _monitor?.SuppressNextChange();
        if (!TryWriteToClipboard(content))
            return false;

        closePopup();

        if (targetWindow != IntPtr.Zero)
            Win32Helpers.SetForegroundWindow(targetWindow);

        await Task.Delay(80).ConfigureAwait(true);

        SendCtrlV();
        return true;
    }

    /// <summary>
    /// Writes content to clipboard only. Does not paste. Returns false if the write fails.
    /// The popup closes on success; stays open on failure.
    /// </summary>
    public bool CopyOnly(ClipboardContent content)
    {
        _monitor?.SuppressNextChange();
        return TryWriteToClipboard(content);
    }

    private static bool TryWriteToClipboard(ClipboardContent content)
    {
        try
        {
            switch (content.Kind)
            {
                case ClipboardItemKind.Image:
                case ClipboardItemKind.Screenshot:
                    return TrySetImage(content.ImagePath);

                case ClipboardItemKind.FileReference:
                    return TrySetFileList(content.FileReferenceJson);

                case ClipboardItemKind.Html:
                case ClipboardItemKind.WordContent:
                    return TrySetHtmlWithTextFallback(content.HtmlText, content.PlainText);

                case ClipboardItemKind.RichText:
                    return TrySetRtfWithTextFallback(content.RichText, content.PlainText);

                case ClipboardItemKind.Url:
                    var url = content.Url ?? content.PlainText;
                    if (string.IsNullOrEmpty(url)) return false;
                    WpfClipboard.SetText(url);
                    return true;

                default:
                    var text = content.PlainText ?? "";
                    WpfClipboard.SetText(text);
                    return true;
            }
        }
        catch
        {
            LoggingService.LogError("PasteService", "Failed to write content to clipboard");
            return false;
        }
    }

    private static bool TrySetImage(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return false;

        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
        bitmap.Freeze();
        WpfClipboard.SetImage(bitmap);
        return true;
    }

    private static bool TrySetFileList(string? fileReferenceJson)
    {
        if (string.IsNullOrEmpty(fileReferenceJson))
            return false;

        var paths = System.Text.Json.JsonSerializer.Deserialize<List<string>>(fileReferenceJson);
        if (paths is null || paths.Count == 0)
            return false;

        var col = new StringCollection();
        col.AddRange(paths.ToArray());
        WpfClipboard.SetFileDropList(col);
        return true;
    }

    private static bool TrySetHtmlWithTextFallback(string? html, string? plainText)
    {
        if (string.IsNullOrEmpty(html) && string.IsNullOrEmpty(plainText))
            return false;

        var data = new WpfDataObject();
        if (!string.IsNullOrEmpty(html))
            data.SetData(WpfDataFormats.Html, html);
        if (!string.IsNullOrEmpty(plainText))
            data.SetData(WpfDataFormats.Text, plainText);
        WpfClipboard.SetDataObject(data);
        return true;
    }

    private static bool TrySetRtfWithTextFallback(string? rtf, string? plainText)
    {
        if (string.IsNullOrEmpty(rtf) && string.IsNullOrEmpty(plainText))
            return false;

        var data = new WpfDataObject();
        if (!string.IsNullOrEmpty(rtf))
            data.SetData(WpfDataFormats.Rtf, rtf);
        if (!string.IsNullOrEmpty(plainText))
            data.SetData(WpfDataFormats.Text, plainText);
        WpfClipboard.SetDataObject(data);
        return true;
    }

    private static void SendCtrlV()
    {
        var inputs = new Win32Helpers.INPUT[]
        {
            MakeKeyInput(Win32Helpers.VK_CONTROL, keyUp: false),
            MakeKeyInput(Win32Helpers.VK_V, keyUp: false),
            MakeKeyInput(Win32Helpers.VK_V, keyUp: true),
            MakeKeyInput(Win32Helpers.VK_CONTROL, keyUp: true),
        };
        Win32Helpers.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Win32Helpers.INPUT>());
    }

    private static Win32Helpers.INPUT MakeKeyInput(int vk, bool keyUp) =>
        new()
        {
            Type = Win32Helpers.INPUT_KEYBOARD,
            Data = new Win32Helpers.INPUTUNION
            {
                Keyboard = new Win32Helpers.KEYBDINPUT
                {
                    Vk = (ushort)vk,
                    Scan = 0,
                    Flags = keyUp ? Win32Helpers.KEYEVENTF_KEYUP : 0,
                    Time = 0,
                    ExtraInfo = IntPtr.Zero
                }
            }
        };
}

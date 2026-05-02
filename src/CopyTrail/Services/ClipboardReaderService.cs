using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;
using CopyTrail.Data.Repositories;
using CopyTrail.Models;
using CopyTrail.Utilities;
using WpfApp = System.Windows.Application;
using WpfClipboard = System.Windows.Clipboard;
using WpfTextFormat = System.Windows.TextDataFormat;

namespace CopyTrail.Services;

public sealed class ClipboardReaderService
{
    private const int PreviewMaxLength = 500;
    private const int LargeContentSnippetLength = 1000;

    private readonly ClipboardRepository _repository;
    private readonly ContentClassifierService _classifier;
    private readonly SourceCaptureService _sourceCapture;
    private readonly FileStorageService _fileStorage;
    private readonly ExclusionService _exclusion;
    private readonly AppSettings _settings;

    /// <summary>Fires on the UI thread after a new clipboard item has been successfully stored.</summary>
    public event EventHandler? ItemStored;

    public bool IsPaused { get; set; }

    public ClipboardReaderService(
        ClipboardRepository repository,
        ContentClassifierService classifier,
        SourceCaptureService sourceCapture,
        FileStorageService fileStorage,
        ExclusionService exclusion,
        AppSettings settings)
    {
        _repository = repository;
        _classifier = classifier;
        _sourceCapture = sourceCapture;
        _fileStorage = fileStorage;
        _exclusion = exclusion;
        _settings = settings;
    }

    public void OnClipboardChanged(object? sender, EventArgs e)
    {
        if (IsPaused) return;
        try
        {
            ProcessClipboard();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CopyTrail] Unexpected error in clipboard processing: {ex.GetType().Name}");
            LoggingService.LogError("ClipboardReaderService", "Unexpected error during clipboard processing", ex);
        }
    }

    private void ProcessClipboard()
    {
        if (!WpfClipboard.ContainsText() && !WpfClipboard.ContainsImage() && !WpfClipboard.ContainsFileDropList())
        {
            Debug.WriteLine("[CopyTrail] Clipboard contains no supported format. Skipping.");
            return;
        }

        // Capture source immediately — GetForegroundWindow should still point to the copy source app.
        ClipboardSource source = _sourceCapture.Capture(ClipboardItemKind.Unknown);

        if (_exclusion.IsExcluded(source.SourceProcessName))
        {
            Debug.WriteLine($"[CopyTrail] Clipboard from excluded process: {source.SourceProcessName}");
            return;
        }

        // Read all available formats on the UI/STA thread.
        string? plainText = ReadPlainText();
        string? htmlText = ReadHtmlText();
        string? rtfText = ReadRtfText();
        BitmapSource? image = ReadImage();
        IReadOnlyList<string>? fileList = ReadFileList();

        if (image?.CanFreeze == true)
            image.Freeze();

        var data = new ClipboardData
        {
            HasImage = image is not null,
            HasFileList = fileList is { Count: > 0 },
            HtmlContent = htmlText,
            PlainText = plainText,
            SourceProcessName = source.SourceProcessName
        };

        ClipboardItemKind kind = _classifier.Classify(data);

        // Additional override: if source is Word and rich content exists, force WordContent.
        if (kind != ClipboardItemKind.WordContent &&
            IsWordProcess(source.SourceProcessName) &&
            (htmlText is not null || rtfText is not null))
        {
            kind = ClipboardItemKind.WordContent;
        }

        // Adjust source kind now that final content kind is known.
        ClipboardSourceKind sourceKind = kind switch
        {
            ClipboardItemKind.Screenshot => ClipboardSourceKind.SystemScreenshot,
            ClipboardItemKind.FileReference => ClipboardSourceKind.SystemFileCopy,
            _ when string.IsNullOrWhiteSpace(source.SourceProcessName) => ClipboardSourceKind.Unknown,
            _ => ClipboardSourceKind.App
        };

        var adjustedSource = new ClipboardSource
        {
            SourceAppName = source.SourceAppName,
            SourceProcessName = source.SourceProcessName,
            SourceProcessPath = source.SourceProcessPath,
            SourceWindowTitle = source.SourceWindowTitle,
            SourceKind = sourceKind,
            Identity = source.Identity
        };

        string contentHash = ComputeHash(kind, plainText, htmlText, image, fileList);
        long sizeBytes = EstimateSize(kind, plainText, htmlText, rtfText, image);
        bool isLarge = (kind == ClipboardItemKind.Image || kind == ClipboardItemKind.Screenshot)
            ? LargeContentPolicy.IsLargeImage(sizeBytes, _settings)
            : LargeContentPolicy.IsLargeText(sizeBytes, _settings);

        string? previewText = BuildPreviewText(kind, plainText, htmlText, fileList);
        string? storedPlainText = TruncateIfLarge(plainText, isLarge);
        string? storedHtmlText = TruncateIfLarge(htmlText, isLarge);
        string? storedRtfText = TruncateIfLarge(rtfText, isLarge);
        string? fileReferenceJson = fileList is { Count: > 0 }
            ? JsonSerializer.Serialize(fileList)
            : null;

        _ = Task.Run(async () =>
            await StoreAsync(
                kind, contentHash, storedPlainText, storedHtmlText, storedRtfText,
                fileReferenceJson, previewText, sizeBytes, isLarge, image, adjustedSource)
            .ConfigureAwait(false));
    }

    private async Task StoreAsync(
        ClipboardItemKind kind,
        string contentHash,
        string? plainText,
        string? htmlText,
        string? rtfText,
        string? fileReferenceJson,
        string? previewText,
        long sizeBytes,
        bool isLarge,
        BitmapSource? image,
        ClipboardSource source)
    {
        try
        {
            string? imagePath = null;
            string? thumbnailPath = null;

            if ((kind == ClipboardItemKind.Image || kind == ClipboardItemKind.Screenshot)
                && image is not null
                && LargeContentPolicy.ShouldStoreImage(_settings))
            {
                var result = await _fileStorage.SaveImageAsync(image, contentHash).ConfigureAwait(false);
                if (result.Success)
                {
                    imagePath = result.ImagePath;
                    thumbnailPath = result.ThumbnailPath;
                }
                else
                {
                    Debug.WriteLine($"[CopyTrail] Image save failed: {result.ErrorMessage}");
                    LoggingService.LogError("ClipboardReaderService", $"Image save failed: {result.ErrorMessage}");
                }
            }

            var existingContent = await _repository.FindContentByHashAsync(contentHash).ConfigureAwait(false);

            long contentId;
            if (existingContent is not null)
            {
                contentId = existingContent.Id;
            }
            else
            {
                var content = new ClipboardContent
                {
                    ContentHash = contentHash,
                    Kind = kind,
                    PlainText = plainText,
                    HtmlText = htmlText,
                    RichText = rtfText,
                    Url = kind == ClipboardItemKind.Url ? plainText : null,
                    ImagePath = imagePath,
                    ThumbnailPath = thumbnailPath,
                    FileReferenceJson = fileReferenceJson,
                    PreviewText = previewText,
                    SizeBytes = sizeBytes,
                    IsLargeContent = isLarge,
                    CreatedAtUtc = DateTime.UtcNow
                };
                contentId = await _repository.AddClipboardContentAsync(content).ConfigureAwait(false);
            }

            var recentEvent = await _repository.FindRecentEventForDuplicateAsync(contentId).ConfigureAwait(false);

            if (recentEvent is not null && IsRapidDuplicate(recentEvent, source))
            {
                int newCount = recentEvent.CopyCount + 1;
                await _repository.UpdateCopyCountAsync(recentEvent.Id, newCount, DateTime.UtcNow).ConfigureAwait(false);
                Debug.WriteLine($"[CopyTrail] Rapid duplicate from same window. CopyCount → {newCount}.");
            }
            else
            {
                var evt = new ClipboardEvent
                {
                    ClipboardContentId = contentId,
                    SourceAppName = source.SourceAppName,
                    SourceProcessName = source.SourceProcessName,
                    SourceProcessPath = source.SourceProcessPath,
                    SourceWindowTitle = source.SourceWindowTitle,
                    SourceKind = source.SourceKind,
                    CopiedAtUtc = DateTime.UtcNow,
                    CopyCount = 1,
                    LastCopiedAtUtc = DateTime.UtcNow
                };
                await _repository.AddClipboardEventAsync(evt).ConfigureAwait(false);
                Debug.WriteLine($"[CopyTrail] Stored. Kind={kind}, Source={source.SourceAppName}");
            }

            WpfApp.Current?.Dispatcher.BeginInvoke(
                () => ItemStored?.Invoke(this, EventArgs.Empty));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CopyTrail] Storage error: {ex.GetType().Name}: {ex.Message}");
            LoggingService.LogError("ClipboardReaderService", "Database storage error", ex);
        }
    }

    private bool IsRapidDuplicate(ClipboardEvent evt, ClipboardSource source)
    {
        if (!string.Equals(evt.SourceProcessName, source.SourceProcessName, StringComparison.OrdinalIgnoreCase))
            return false;
        if (!string.Equals(evt.SourceWindowTitle, source.SourceWindowTitle, StringComparison.Ordinal))
            return false;
        var window = TimeSpan.FromSeconds(_settings.RapidDuplicateWindowSeconds);
        return (DateTime.UtcNow - evt.LastCopiedAtUtc) <= window;
    }

    private static string? ReadPlainText()
    {
        try
        {
            if (WpfClipboard.ContainsText(WpfTextFormat.UnicodeText))
                return WpfClipboard.GetText(WpfTextFormat.UnicodeText);
            if (WpfClipboard.ContainsText())
                return WpfClipboard.GetText();
            return null;
        }
        catch { return null; }
    }

    private static string? ReadHtmlText()
    {
        try
        {
            if (!WpfClipboard.ContainsText(WpfTextFormat.Html))
                return null;
            string raw = WpfClipboard.GetText(WpfTextFormat.Html);
            return StripHtmlClipboardHeader(raw);
        }
        catch { return null; }
    }

    private static string? ReadRtfText()
    {
        try
        {
            return WpfClipboard.ContainsText(WpfTextFormat.Rtf)
                ? WpfClipboard.GetText(WpfTextFormat.Rtf)
                : null;
        }
        catch { return null; }
    }

    private static BitmapSource? ReadImage()
    {
        try
        {
            return WpfClipboard.ContainsImage() ? WpfClipboard.GetImage() : null;
        }
        catch { return null; }
    }

    private static IReadOnlyList<string>? ReadFileList()
    {
        try
        {
            if (!WpfClipboard.ContainsFileDropList())
                return null;
            StringCollection files = WpfClipboard.GetFileDropList();
            var list = new List<string>(files.Count);
            foreach (string? f in files)
                if (f is not null) list.Add(f);
            return list.Count > 0 ? list : null;
        }
        catch { return null; }
    }

    // The Windows "HTML Format" clipboard type prepends metadata headers before the HTML.
    // Find the first '<' to skip those headers.
    private static string? StripHtmlClipboardHeader(string? raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int idx = raw.IndexOf('<');
        return idx >= 0 ? raw[idx..] : raw;
    }

    private static string ComputeHash(
        ClipboardItemKind kind,
        string? plainText,
        string? htmlText,
        BitmapSource? image,
        IReadOnlyList<string>? fileList)
    {
        if ((kind == ClipboardItemKind.Image || kind == ClipboardItemKind.Screenshot) && image is not null)
            return ComputeImageHash(image);

        if (fileList is { Count: > 0 })
        {
            string sorted = string.Join("|", fileList.OrderBy(f => f, StringComparer.OrdinalIgnoreCase));
            return Hashing.Sha256OfText(sorted);
        }

        return Hashing.Sha256OfText(plainText ?? htmlText ?? string.Empty);
    }

    private static string ComputeImageHash(BitmapSource image)
    {
        try
        {
            int stride = (image.PixelWidth * image.Format.BitsPerPixel + 7) / 8;
            byte[] pixels = new byte[image.PixelHeight * stride];
            image.CopyPixels(pixels, stride, 0);
            return Hashing.Sha256OfBytes(pixels);
        }
        catch
        {
            // Fallback so a hash failure never crashes the pipeline.
            return Hashing.Sha256OfText(DateTime.UtcNow.Ticks.ToString());
        }
    }

    private static long EstimateSize(
        ClipboardItemKind kind,
        string? plainText,
        string? htmlText,
        string? rtfText,
        BitmapSource? image)
    {
        if ((kind == ClipboardItemKind.Image || kind == ClipboardItemKind.Screenshot) && image is not null)
            return (long)image.PixelWidth * image.PixelHeight * Math.Max(1, image.Format.BitsPerPixel / 8);

        return Encoding.UTF8.GetByteCount(plainText ?? "")
             + Encoding.UTF8.GetByteCount(htmlText ?? "")
             + Encoding.UTF8.GetByteCount(rtfText ?? "");
    }

    private static string? BuildPreviewText(
        ClipboardItemKind kind,
        string? plainText,
        string? htmlText,
        IReadOnlyList<string>? fileList)
    {
        if (kind == ClipboardItemKind.Image || kind == ClipboardItemKind.Screenshot)
            return null;

        if (kind == ClipboardItemKind.FileReference && fileList is { Count: > 0 })
        {
            var names = fileList.Select(Path.GetFileName).Take(3);
            string joined = string.Join(", ", names);
            return fileList.Count > 3 ? $"{joined} +{fileList.Count - 3} more" : joined;
        }

        string? text = plainText ?? htmlText;
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        return text.Length <= PreviewMaxLength ? text : text[..PreviewMaxLength] + "…";
    }

    private static string? TruncateIfLarge(string? text, bool isLarge)
    {
        if (!isLarge || text is null) return text;
        return text.Length > LargeContentSnippetLength ? text[..LargeContentSnippetLength] : text;
    }

    private static bool IsWordProcess(string? processName) =>
        processName is not null &&
        (processName.Equals("WINWORD", StringComparison.OrdinalIgnoreCase) ||
         processName.Equals("WINWORD.EXE", StringComparison.OrdinalIgnoreCase));
}

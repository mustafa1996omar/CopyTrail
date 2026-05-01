using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CopyTrail.Models;
using CopyTrail.Utilities;

namespace CopyTrail.Services;

public sealed class FileStorageService
{
    private readonly AppSettings _settings;

    public FileStorageService(AppSettings settings)
    {
        _settings = settings;
    }

    public string GetMediaRoot() => ImageUtilities.GetMediaRoot();

    public string GetThumbnailRoot() => ImageUtilities.GetThumbnailRoot();

    public async Task<StoredImageResult> SaveImageAsync(BitmapSource image, string contentHash)
    {
        try
        {
            FreezeIfPossible(image);

            long sizeBytes = EstimateImageBytes(image);
            bool isLarge = LargeContentPolicy.IsLargeImage(sizeBytes, _settings);

            EnsureDirectories();

            string? imagePath = null;
            if (!isLarge)
            {
                imagePath = ImageUtilities.GetImagePath(contentHash);
                if (!File.Exists(imagePath))
                    await SaveBitmapAsPngAsync(image, imagePath).ConfigureAwait(false);
            }

            BitmapSource thumbnail = CreateThumbnail(image, 200, 120);
            FreezeIfPossible(thumbnail);

            string thumbPath = ImageUtilities.GetThumbnailPath(contentHash);
            if (!File.Exists(thumbPath))
                await SaveBitmapAsPngAsync(thumbnail, thumbPath).ConfigureAwait(false);

            return new StoredImageResult
            {
                Success = true,
                ImagePath = imagePath,
                ThumbnailPath = thumbPath,
                IsLarge = isLarge,
            };
        }
        catch (Exception ex)
        {
            return StoredImageResult.Failed(ex.Message);
        }
    }

    public async Task<string?> SaveThumbnailAsync(BitmapSource image, string contentHash)
    {
        try
        {
            FreezeIfPossible(image);
            EnsureDirectories();

            BitmapSource thumbnail = CreateThumbnail(image, 200, 120);
            FreezeIfPossible(thumbnail);

            string thumbPath = ImageUtilities.GetThumbnailPath(contentHash);
            if (!File.Exists(thumbPath))
                await SaveBitmapAsPngAsync(thumbnail, thumbPath).ConfigureAwait(false);

            return thumbPath;
        }
        catch
        {
            return null;
        }
    }

    public void DeleteMediaFileIfExists(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;
        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            // silently ignore — delete failures must not crash the app
        }
    }

    public async Task ClearAllMediaAsync()
    {
        await Task.Run(() =>
        {
            string root = GetMediaRoot();
            if (!Directory.Exists(root))
                return;

            foreach (string file in Directory.GetFiles(root, "*.*", SearchOption.AllDirectories))
            {
                try { File.Delete(file); } catch { }
            }
        }).ConfigureAwait(false);
    }

    private void EnsureDirectories()
    {
        Directory.CreateDirectory(GetMediaRoot());
        Directory.CreateDirectory(GetThumbnailRoot());
    }

    private static BitmapSource CreateThumbnail(BitmapSource source, int maxWidth, int maxHeight)
    {
        if (source.PixelWidth == 0 || source.PixelHeight == 0)
            return source;

        double scaleX = (double)maxWidth / source.PixelWidth;
        double scaleY = (double)maxHeight / source.PixelHeight;
        double scale = Math.Min(Math.Min(scaleX, scaleY), 1.0);

        if (scale >= 1.0)
            return source;

        var transform = new ScaleTransform(scale, scale);
        transform.Freeze();
        var thumbnail = new TransformedBitmap(source, transform);
        thumbnail.Freeze();
        return thumbnail;
    }

    private static long EstimateImageBytes(BitmapSource image) =>
        (long)image.PixelWidth * image.PixelHeight * Math.Max(1, image.Format.BitsPerPixel / 8);

    private static async Task SaveBitmapAsPngAsync(BitmapSource image, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));

        using var ms = new MemoryStream();
        encoder.Save(ms);
        byte[] bytes = ms.ToArray();

        await File.WriteAllBytesAsync(path, bytes).ConfigureAwait(false);
    }

    private static void FreezeIfPossible(BitmapSource bitmap)
    {
        if (bitmap.CanFreeze && !bitmap.IsFrozen)
            bitmap.Freeze();
    }
}

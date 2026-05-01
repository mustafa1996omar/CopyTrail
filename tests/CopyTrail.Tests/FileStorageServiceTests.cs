using System.IO;
using CopyTrail.Models;
using CopyTrail.Utilities;
using Xunit;

namespace CopyTrail.Tests;

public sealed class FileStorageServiceTests
{
    // ── Media path generation ────────────────────────────────────────────────

    [Fact]
    public void GetMediaRoot_IsUnderLocalAppData()
    {
        string root = ImageUtilities.GetMediaRoot();
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        Assert.StartsWith(localAppData, root, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(Path.Combine("CopyTrail", "media"), root, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetThumbnailRoot_IsSubfolderOfMediaRoot()
    {
        string mediaRoot = ImageUtilities.GetMediaRoot();
        string thumbRoot = ImageUtilities.GetThumbnailRoot();

        Assert.StartsWith(mediaRoot, thumbRoot, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("thumbs", thumbRoot, StringComparison.OrdinalIgnoreCase);
    }

    // ── Thumbnail path generation ─────────────────────────────────────────────

    [Fact]
    public void GetThumbnailPath_IsUnderThumbnailRoot()
    {
        string thumbRoot = ImageUtilities.GetThumbnailRoot();
        string path = ImageUtilities.GetThumbnailPath("abc123");

        Assert.StartsWith(thumbRoot, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetThumbnailPath_ContainsHashAndThumbSuffix()
    {
        string path = ImageUtilities.GetThumbnailPath("abc123");

        Assert.Contains("abc123", path);
        Assert.Contains("_thumb", path);
        Assert.EndsWith(".png", path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetImagePath_IsUnderMediaRoot()
    {
        string mediaRoot = ImageUtilities.GetMediaRoot();
        string path = ImageUtilities.GetImagePath("abc123");

        Assert.StartsWith(mediaRoot, path, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetImagePath_UsesPngExtension()
    {
        string path = ImageUtilities.GetImagePath("abc123");
        Assert.EndsWith(".png", path, StringComparison.OrdinalIgnoreCase);
    }

    // ── Safe filename behavior ────────────────────────────────────────────────

    [Fact]
    public void GetSafeFilename_PreservesAlphanumericsHyphensUnderscores()
    {
        string result = ImageUtilities.GetSafeFilename("aB3-_hash");
        Assert.Equal("aB3-_hash", result);
    }

    [Fact]
    public void GetSafeFilename_StripsInvalidChars()
    {
        string result = ImageUtilities.GetSafeFilename("ab/cd\\ef:gh?ij");
        Assert.Equal("abcdefghij", result);
    }

    [Fact]
    public void GetSafeFilename_EmptyInput_ReturnsFallback()
    {
        string result = ImageUtilities.GetSafeFilename("");
        Assert.False(string.IsNullOrEmpty(result));
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void GetSafeFilename_AllInvalidChars_ReturnsFallback()
    {
        string result = ImageUtilities.GetSafeFilename("!@#$%^&*()");
        Assert.False(string.IsNullOrEmpty(result));
        Assert.DoesNotContain("/", result);
        Assert.DoesNotContain("\\", result);
    }

    [Fact]
    public void GetSafeFilename_SameHashProducesSameFilename()
    {
        string hash = "d41d8cd98f00b204e9800998ecf8427e";
        Assert.Equal(ImageUtilities.GetSafeFilename(hash), ImageUtilities.GetSafeFilename(hash));
    }

    // ── Size policy behavior ──────────────────────────────────────────────────

    [Fact]
    public void IsLargeImage_BelowLimit_ReturnsFalse()
    {
        var settings = new AppSettings { MaxImageBytes = 20 * 1024 * 1024 };
        Assert.False(LargeContentPolicy.IsLargeImage(1024, settings));
    }

    [Fact]
    public void IsLargeImage_AboveLimit_ReturnsTrue()
    {
        var settings = new AppSettings { MaxImageBytes = 1024 };
        Assert.True(LargeContentPolicy.IsLargeImage(2048, settings));
    }

    [Fact]
    public void IsLargeImage_ExactlyAtLimit_ReturnsFalse()
    {
        var settings = new AppSettings { MaxImageBytes = 1024 };
        Assert.False(LargeContentPolicy.IsLargeImage(1024, settings));
    }

    // ── StoredImageResult ─────────────────────────────────────────────────────

    [Fact]
    public void StoredImageResult_Failed_SetsSuccessFalseAndMessage()
    {
        var result = StoredImageResult.Failed("encoding error");

        Assert.False(result.Success);
        Assert.Equal("encoding error", result.ErrorMessage);
        Assert.Null(result.ImagePath);
        Assert.Null(result.ThumbnailPath);
    }

    [Fact]
    public void StoredImageResult_SuccessfulLargeImage_HasNullImagePath()
    {
        var result = new StoredImageResult
        {
            Success = true,
            IsLarge = true,
            ImagePath = null,
            ThumbnailPath = "/some/thumb.png",
        };

        Assert.True(result.Success);
        Assert.True(result.IsLarge);
        Assert.Null(result.ImagePath);
        Assert.NotNull(result.ThumbnailPath);
    }
}

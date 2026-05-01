using System.IO;
using CopyTrail.Data;
using CopyTrail.Data.Repositories;
using CopyTrail.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CopyTrail.Tests;

public sealed class ClipboardRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly CopyTrailDbContext _context;
    private readonly ClipboardRepository _repository;

    public ClipboardRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"CopyTrail_test_{Guid.NewGuid():N}.db");
        _context = new CopyTrailDbContext(_dbPath);
        DatabaseInitializer.Initialize(_context);
        _repository = new ClipboardRepository(_context);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        GC.Collect();
        GC.WaitForPendingFinalizers();

        foreach (var related in new[] { _dbPath, _dbPath + "-shm", _dbPath + "-wal" })
        {
            if (File.Exists(related))
                try { File.Delete(related); } catch { }
        }
    }

    [Fact]
    public void DatabaseInitializes_CreatesFile()
    {
        Assert.True(File.Exists(_dbPath));
    }

    [Fact]
    public async Task AddClipboardContentAsync_ReturnsNonZeroId()
    {
        var content = MakeContent("hello world", "abc123");
        var id = await _repository.AddClipboardContentAsync(content);
        Assert.True(id > 0);
        Assert.Equal(id, content.Id);
    }

    [Fact]
    public async Task AddClipboardEventAsync_ReturnsNonZeroId()
    {
        var content = MakeContent("test", "hash1");
        var contentId = await _repository.AddClipboardContentAsync(content);

        var evt = MakeEvent(contentId, "Chrome");
        var eventId = await _repository.AddClipboardEventAsync(evt);
        Assert.True(eventId > 0);
        Assert.Equal(eventId, evt.Id);
    }

    [Fact]
    public async Task FindContentByHashAsync_ReturnsInsertedContent()
    {
        var content = MakeContent("find me", "unique_hash_99");
        await _repository.AddClipboardContentAsync(content);

        var found = await _repository.FindContentByHashAsync("unique_hash_99");
        Assert.NotNull(found);
        Assert.Equal("unique_hash_99", found.ContentHash);
        Assert.Equal("find me", found.PlainText);
    }

    [Fact]
    public async Task FindContentByHashAsync_ReturnsNullForMissingHash()
    {
        var result = await _repository.FindContentByHashAsync("does_not_exist");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetRecentTimelineItemsAsync_ReturnsInsertedItems()
    {
        var content = MakeContent("timeline test", "hash_tl");
        await _repository.AddClipboardContentAsync(content);
        var evt = MakeEvent(content.Id, "VSCode");
        await _repository.AddClipboardEventAsync(evt);

        var items = await _repository.GetRecentTimelineItemsAsync(50);
        Assert.NotEmpty(items);
        Assert.Contains(items, i => i.Content.ContentHash == "hash_tl");
    }

    [Fact]
    public async Task GetRecentTimelineItemsAsync_RespectsLimit()
    {
        for (int i = 0; i < 5; i++)
        {
            var c = MakeContent($"text {i}", $"hash_{i}");
            await _repository.AddClipboardContentAsync(c);
            await _repository.AddClipboardEventAsync(MakeEvent(c.Id));
        }

        var items = await _repository.GetRecentTimelineItemsAsync(3);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task SearchTimelineItemsAsync_FindsMatchingItem()
    {
        var content = MakeContent("important secret phrase", "hash_search");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        var results = await _repository.SearchTimelineItemsAsync("secret phrase", 50);
        Assert.NotEmpty(results);
        Assert.Contains(results, i => i.Content.ContentHash == "hash_search");
    }

    [Fact]
    public async Task SearchTimelineItemsAsync_ReturnsEmptyForNoMatch()
    {
        var content = MakeContent("something else entirely", "hash_nomatch");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        var results = await _repository.SearchTimelineItemsAsync("xyzzy_not_present", 50);
        Assert.Empty(results);
    }

    [Fact]
    public async Task FindRecentEventForDuplicateAsync_ReturnsEventForContent()
    {
        var content = MakeContent("dup check", "hash_dup");
        await _repository.AddClipboardContentAsync(content);
        var evt = MakeEvent(content.Id, "Word");
        await _repository.AddClipboardEventAsync(evt);

        var found = await _repository.FindRecentEventForDuplicateAsync(content.Id);
        Assert.NotNull(found);
        Assert.Equal(content.Id, found.ClipboardContentId);
        Assert.Equal("Word", found.SourceAppName);
    }

    [Fact]
    public async Task FindRecentEventForDuplicateAsync_ReturnsNullForMissingContent()
    {
        var result = await _repository.FindRecentEventForDuplicateAsync(999999);
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateCopyCountAsync_IncrementsCopyCount()
    {
        var content = MakeContent("copy me", "hash_copy");
        await _repository.AddClipboardContentAsync(content);
        var evt = MakeEvent(content.Id);
        await _repository.AddClipboardEventAsync(evt);

        var updatedAt = DateTime.UtcNow;
        await _repository.UpdateCopyCountAsync(evt.Id, 3, updatedAt);

        var found = await _repository.FindRecentEventForDuplicateAsync(content.Id);
        Assert.NotNull(found);
        Assert.Equal(3, found.CopyCount);
    }

    [Fact]
    public async Task ClearAllAsync_RemovesAllItems()
    {
        var content = MakeContent("will be cleared", "hash_clear");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.ClearAllAsync();

        var items = await _repository.GetRecentTimelineItemsAsync(50);
        Assert.Empty(items);
    }

    private static ClipboardContent MakeContent(string text, string hash) =>
        new()
        {
            ContentHash = hash,
            Kind = ClipboardItemKind.Text,
            PlainText = text,
            PreviewText = text,
            SizeBytes = text.Length,
            CreatedAtUtc = DateTime.UtcNow,
        };

    private static ClipboardEvent MakeEvent(long contentId, string? sourceApp = null) =>
        new()
        {
            ClipboardContentId = contentId,
            SourceAppName = sourceApp,
            SourceKind = ClipboardSourceKind.Unknown,
            CopiedAtUtc = DateTime.UtcNow,
            LastCopiedAtUtc = DateTime.UtcNow,
            CopyCount = 1,
        };
}

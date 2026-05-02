using System.IO;
using CopyTrail.Data;
using CopyTrail.Data.Repositories;
using CopyTrail.Models;
using Microsoft.Data.Sqlite;
using Xunit;

namespace CopyTrail.Tests;

public sealed class PinningRepositoryTests : IDisposable
{
    private readonly string _dbPath;
    private readonly CopyTrailDbContext _context;
    private readonly ClipboardRepository _repository;

    public PinningRepositoryTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"CopyTrail_pin_test_{Guid.NewGuid():N}.db");
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
    public async Task PinContentAsync_SetsIsPinnedTrue()
    {
        var content = MakeContent("pin me", "hash_pin1");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.PinContentAsync(content.Id);

        var pinned = await _repository.GetPinnedItemsAsync();
        Assert.Single(pinned);
        Assert.Equal("hash_pin1", pinned[0].Content.ContentHash);
        Assert.True(pinned[0].Content.IsPinned);
    }

    [Fact]
    public async Task UnpinContentAsync_SetsIsPinnedFalse()
    {
        var content = MakeContent("unpin me", "hash_unpin1");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.PinContentAsync(content.Id);
        await _repository.UnpinContentAsync(content.Id);

        var pinned = await _repository.GetPinnedItemsAsync();
        Assert.Empty(pinned);
    }

    [Fact]
    public async Task GetPinnedItemsAsync_ReturnsOnlyPinnedItems()
    {
        var c1 = MakeContent("not pinned", "hash_np1");
        await _repository.AddClipboardContentAsync(c1);
        await _repository.AddClipboardEventAsync(MakeEvent(c1.Id));

        var c2 = MakeContent("will be pinned", "hash_p1");
        await _repository.AddClipboardContentAsync(c2);
        await _repository.AddClipboardEventAsync(MakeEvent(c2.Id));

        var c3 = MakeContent("also pinned", "hash_p2");
        await _repository.AddClipboardContentAsync(c3);
        await _repository.AddClipboardEventAsync(MakeEvent(c3.Id));

        await _repository.PinContentAsync(c2.Id);
        await _repository.PinContentAsync(c3.Id);

        var pinned = await _repository.GetPinnedItemsAsync();
        Assert.Equal(2, pinned.Count);
        Assert.All(pinned, item => Assert.True(item.Content.IsPinned));
        Assert.DoesNotContain(pinned, item => item.Content.ContentHash == "hash_np1");
    }

    [Fact]
    public async Task GetPinnedItemsAsync_ReturnsEmptyWhenNonePinned()
    {
        var content = MakeContent("no pins here", "hash_np2");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        var pinned = await _repository.GetPinnedItemsAsync();
        Assert.Empty(pinned);
    }

    [Fact]
    public async Task PinContentAsync_IdempotentWhenAlreadyPinned()
    {
        var content = MakeContent("double pin", "hash_dp1");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.PinContentAsync(content.Id);
        await _repository.PinContentAsync(content.Id);

        var pinned = await _repository.GetPinnedItemsAsync();
        Assert.Single(pinned);
    }

    [Fact]
    public async Task UnpinContentAsync_IdempotentWhenNotPinned()
    {
        var content = MakeContent("double unpin", "hash_du1");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.UnpinContentAsync(content.Id);
        await _repository.UnpinContentAsync(content.Id);

        var pinned = await _repository.GetPinnedItemsAsync();
        Assert.Empty(pinned);
    }

    [Fact]
    public async Task GetRecentTimelineItemsAsync_ReturnsPinnedItemsFirst()
    {
        var c1 = MakeContent("regular", "hash_r1");
        await _repository.AddClipboardContentAsync(c1);
        await _repository.AddClipboardEventAsync(MakeEvent(c1.Id));

        var c2 = MakeContent("pinned", "hash_p_first");
        await _repository.AddClipboardContentAsync(c2);
        await _repository.AddClipboardEventAsync(MakeEvent(c2.Id));

        await _repository.PinContentAsync(c2.Id);

        var items = await _repository.GetRecentTimelineItemsAsync(50);
        Assert.True(items.Count >= 2);
        Assert.True(items[0].Content.IsPinned);
    }

    [Fact]
    public async Task ClearAllAsync_DefaultKeepsPinnedFalse_DeletesEverything()
    {
        var c1 = MakeContent("regular to delete", "hash_ctd1");
        await _repository.AddClipboardContentAsync(c1);
        await _repository.AddClipboardEventAsync(MakeEvent(c1.Id));

        var c2 = MakeContent("pinned to delete", "hash_ctd2");
        await _repository.AddClipboardContentAsync(c2);
        await _repository.AddClipboardEventAsync(MakeEvent(c2.Id));
        await _repository.PinContentAsync(c2.Id);

        await _repository.ClearAllAsync(keepPinned: false);

        var items = await _repository.GetRecentTimelineItemsAsync(50);
        Assert.Empty(items);
    }

    [Fact]
    public async Task ClearAllAsync_KeepPinnedTrue_PreservesPinnedItems()
    {
        var c1 = MakeContent("regular to clear", "hash_rtc1");
        await _repository.AddClipboardContentAsync(c1);
        await _repository.AddClipboardEventAsync(MakeEvent(c1.Id));

        var c2 = MakeContent("pinned to keep", "hash_ptk1");
        await _repository.AddClipboardContentAsync(c2);
        await _repository.AddClipboardEventAsync(MakeEvent(c2.Id));
        await _repository.PinContentAsync(c2.Id);

        await _repository.ClearAllAsync(keepPinned: true);

        var items = await _repository.GetRecentTimelineItemsAsync(50);
        Assert.Single(items);
        Assert.Equal("hash_ptk1", items[0].Content.ContentHash);
        Assert.True(items[0].Content.IsPinned);
    }

    [Fact]
    public async Task ClearAllAsync_KeepPinnedTrue_DeletesAllUnpinnedItems()
    {
        for (int i = 0; i < 3; i++)
        {
            var c = MakeContent($"regular {i}", $"hash_reg_{i}");
            await _repository.AddClipboardContentAsync(c);
            await _repository.AddClipboardEventAsync(MakeEvent(c.Id));
        }

        var pinned = MakeContent("keep me", "hash_keep1");
        await _repository.AddClipboardContentAsync(pinned);
        await _repository.AddClipboardEventAsync(MakeEvent(pinned.Id));
        await _repository.PinContentAsync(pinned.Id);

        await _repository.ClearAllAsync(keepPinned: true);

        var items = await _repository.GetRecentTimelineItemsAsync(50);
        Assert.Single(items);
        Assert.Equal("hash_keep1", items[0].Content.ContentHash);
    }

    [Fact]
    public async Task ClearAllAsync_KeepPinnedTrue_NoItemsLeft_WhenNonePinned()
    {
        var content = MakeContent("no pin, will be cleared", "hash_npc1");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.ClearAllAsync(keepPinned: true);

        var items = await _repository.GetRecentTimelineItemsAsync(50);
        Assert.Empty(items);
    }

    [Fact]
    public async Task TogglePinRoundTrip_PinThenUnpin_ItemNotInPinnedList()
    {
        var content = MakeContent("round trip", "hash_rt1");
        await _repository.AddClipboardContentAsync(content);
        await _repository.AddClipboardEventAsync(MakeEvent(content.Id));

        await _repository.PinContentAsync(content.Id);
        var afterPin = await _repository.GetPinnedItemsAsync();
        Assert.Single(afterPin);

        await _repository.UnpinContentAsync(content.Id);
        var afterUnpin = await _repository.GetPinnedItemsAsync();
        Assert.Empty(afterUnpin);
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

    private static ClipboardEvent MakeEvent(long contentId) =>
        new()
        {
            ClipboardContentId = contentId,
            SourceKind = ClipboardSourceKind.Unknown,
            CopiedAtUtc = DateTime.UtcNow,
            LastCopiedAtUtc = DateTime.UtcNow,
            CopyCount = 1,
        };
}

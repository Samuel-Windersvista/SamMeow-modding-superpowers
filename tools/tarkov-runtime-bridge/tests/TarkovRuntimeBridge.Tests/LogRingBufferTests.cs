using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class LogRingBufferTests
{
    private static readonly long Ticks =
        new DateTime(2026, 9, 16, 0, 0, 0, DateTimeKind.Utc).Ticks;

    private static long Append(LogRingBuffer buffer, string level, string text)
    {
        return buffer.Append(Ticks, level, "src", text);
    }

    // ---------- seq ----------

    [Fact]
    public void Seq_starts_at_one_and_is_monotonic()
    {
        var buffer = new LogRingBuffer();

        Assert.Equal(1, Append(buffer, "warning", "a"));
        Assert.Equal(2, Append(buffer, "warning", "b"));
        Assert.Equal(3, Append(buffer, "error", "c"));
        Assert.Equal(3, buffer.LatestSeq);
    }

    [Fact]
    public void Entry_carries_level_source_text_and_timestamp()
    {
        var buffer = new LogRingBuffer();
        buffer.Append(Ticks, "warning", "Unity", "boom");

        var page = buffer.Snapshot(0, 100);

        Assert.Single(page.Entries);
        Assert.Equal("warning", page.Entries[0].Level);
        Assert.Equal("Unity", page.Entries[0].Source);
        Assert.Equal("boom", page.Entries[0].Text);
        Assert.Equal(Ticks, page.Entries[0].TimestampUtcTicks);
    }

    [Fact]
    public void Null_level_source_and_text_become_empty_strings()
    {
        var buffer = new LogRingBuffer();
        buffer.Append(Ticks, null, null, null);

        var page = buffer.Snapshot(0, 100);

        Assert.Single(page.Entries);
        Assert.Equal(string.Empty, page.Entries[0].Level);
        Assert.Equal(string.Empty, page.Entries[0].Source);
        Assert.Equal(string.Empty, page.Entries[0].Text);
    }

    [Fact]
    public void Default_capacity_and_limit_match_contract()
    {
        Assert.Equal(1000, LogRingBuffer.DefaultCapacity);
        Assert.Equal(100, LogRingBuffer.DefaultLimit);
        Assert.Equal(1000, new LogRingBuffer().Capacity);
    }

    // ---------- snapshot / since ----------

    [Fact]
    public void Snapshot_returns_only_entries_newer_than_since()
    {
        var buffer = new LogRingBuffer();
        Append(buffer, "warning", "a");
        Append(buffer, "warning", "b");
        Append(buffer, "warning", "c");

        var page = buffer.Snapshot(1, 100);

        Assert.Equal(3, page.Seq);
        Assert.Equal(0, page.Dropped);
        Assert.Equal(2, page.Entries.Length);
        Assert.Equal(2, page.Entries[0].Seq);
        Assert.Equal(3, page.Entries[1].Seq);
    }

    [Fact]
    public void Snapshot_limit_truncates_but_seq_still_reports_latest()
    {
        var buffer = new LogRingBuffer();
        Append(buffer, "warning", "a");
        Append(buffer, "warning", "b");
        Append(buffer, "warning", "c");

        var page = buffer.Snapshot(0, 2);

        Assert.Equal(3, page.Seq);
        Assert.Equal(2, page.Entries.Length);
        Assert.Equal(1, page.Entries[0].Seq);
        Assert.Equal(2, page.Entries[1].Seq);
    }

    [Fact]
    public void Snapshot_limit_defaults_when_non_positive()
    {
        var buffer = new LogRingBuffer();
        Append(buffer, "warning", "a");

        Assert.Single(buffer.Snapshot(0, 0).Entries);
        Assert.Single(buffer.Snapshot(0, -5).Entries);
    }

    [Fact]
    public void Negative_since_is_treated_as_zero()
    {
        var buffer = new LogRingBuffer();
        Append(buffer, "warning", "a");

        var page = buffer.Snapshot(-10, 100);

        Assert.Equal(0, page.Dropped);
        Assert.Single(page.Entries);
    }

    [Fact]
    public void Empty_buffer_reports_zero_seq_and_no_drop()
    {
        var page = new LogRingBuffer().Snapshot(0, 100);

        Assert.Equal(0, page.Seq);
        Assert.Equal(0, page.Dropped);
        Assert.Empty(page.Entries);
    }

    // ---------- ring eviction / dropped ----------

    [Fact]
    public void Ring_keeps_capacity_and_evicts_oldest()
    {
        var buffer = new LogRingBuffer(capacity: 3);
        for (var i = 1; i <= 5; i++)
        {
            Append(buffer, "warning", i.ToString());
        }

        var page = buffer.Snapshot(0, 100);

        Assert.Equal(5, page.Seq);
        Assert.Equal(3, page.Entries.Length);
        Assert.Equal(3, page.Entries[0].Seq);
        Assert.Equal(4, page.Entries[1].Seq);
        Assert.Equal(5, page.Entries[2].Seq);
        Assert.Equal("5", page.Entries[2].Text);
    }

    [Fact]
    public void Since_older_than_buffer_reports_dropped_and_starts_at_oldest()
    {
        var buffer = new LogRingBuffer(capacity: 3);
        for (var i = 1; i <= 5; i++)
        {
            Append(buffer, "warning", i.ToString());
        }

        var page = buffer.Snapshot(0, 100);

        Assert.Equal(2, page.Dropped);
        Assert.Equal(3, page.Entries[0].Seq);
    }

    [Fact]
    public void Since_at_oldest_boundary_reports_no_drop()
    {
        var buffer = new LogRingBuffer(capacity: 3);
        for (var i = 1; i <= 5; i++)
        {
            Append(buffer, "warning", i.ToString());
        }

        var page = buffer.Snapshot(2, 100);

        Assert.Equal(0, page.Dropped);
        Assert.Equal(3, page.Entries[0].Seq);
    }

    [Fact]
    public void Capacity_is_clamped_to_at_least_one()
    {
        var buffer = new LogRingBuffer(capacity: 0);

        Assert.Equal(1, buffer.Capacity);
        Append(buffer, "warning", "a");
        Append(buffer, "warning", "b");

        var page = buffer.Snapshot(0, 100);
        Assert.Single(page.Entries);
        Assert.Equal(2, page.Entries[0].Seq);
    }

    // ---------- concurrency ----------

    [Fact]
    public async Task Concurrent_appends_are_lossless_and_seq_is_unique()
    {
        // 红线：BepInEx 日志来自任意线程；写入路径必须线程安全且不丢条。
        const int threadCount = 8;
        const int perThread = 500;
        var buffer = new LogRingBuffer(capacity: threadCount * perThread);

        var tasks = new Task[threadCount];
        for (var t = 0; t < threadCount; t++)
        {
            var id = t;
            tasks[t] = Task.Run(() =>
            {
                for (var i = 0; i < perThread; i++)
                {
                    buffer.Append(Ticks, "warning", "src", "t" + id);
                }
            });
        }

        await Task.WhenAll(tasks);

        Assert.Equal(threadCount * perThread, buffer.LatestSeq);

        var page = buffer.Snapshot(0, threadCount * perThread);
        Assert.Equal(threadCount * perThread, page.Entries.Length);
        Assert.Equal(0, page.Dropped);

        var seen = new HashSet<long>();
        foreach (var entry in page.Entries)
        {
            Assert.True(seen.Add(entry.Seq), $"duplicate seq {entry.Seq}");
        }
    }
}

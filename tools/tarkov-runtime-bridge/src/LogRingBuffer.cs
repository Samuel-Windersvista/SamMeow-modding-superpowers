using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 增量拉取结果：<see cref="Seq"/> 为桥进程内最新 seq，<see cref="Dropped"/> 为因 since
/// 过旧而丢失的条数，<see cref="Entries"/> 为本次返回的增量（未做级别过滤，级别过滤在
/// <see cref="BridgePayloads.BuildLogs"/> 的查询侧）。
/// </summary>
internal readonly struct LogEntryPage
{
    internal LogEntryPage(long seq, int dropped, LogEntry[] entries)
    {
        Seq = seq;
        Dropped = dropped;
        Entries = entries;
    }

    internal long Seq { get; }

    internal int Dropped { get; }

    internal LogEntry[] Entries { get; }
}

/// <summary>
/// 线程安全日志环形缓冲（容量默认 1000）：BepInEx 日志回调来自任意线程（写），
/// HTTP 线程读。seq 桥进程内单调递增；超出容量后自然淘汰最旧日志。
/// 结构照抄 <see cref="RaidEventBuffer"/>（同一套 seq / dropped / limit 语义）。
///
/// 红线：<see cref="Append"/> 只写内存、零 I/O、绝不抛异常（异常会穿透到游戏日志调用方）。
/// </summary>
internal sealed class LogRingBuffer
{
    internal const int DefaultCapacity = 1000;

    /// <summary>未指定 limit 时的默认返回条数。</summary>
    internal const int DefaultLimit = 100;

    private readonly object gate = new object();
    private readonly LogEntry[] ring;

    /// <summary>最旧日志的环下标。</summary>
    private int head;

    private int size;

    /// <summary>已分配的最新 seq；0 表示尚无日志。</summary>
    private long latestSeq;

    internal LogRingBuffer(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
        {
            capacity = 1;
        }

        ring = new LogEntry[capacity];
    }

    internal int Capacity
    {
        get { return ring.Length; }
    }

    internal long LatestSeq
    {
        get
        {
            lock (gate)
            {
                return latestSeq;
            }
        }
    }

    /// <summary>写入一条日志并返回其 seq。只写内存：无 I/O、无分配之外的副作用、不抛出。</summary>
    internal long Append(long timestampUtcTicks, string level, string source, string text)
    {
        lock (gate)
        {
            latestSeq++;
            var entry = new LogEntry(latestSeq, timestampUtcTicks, level, source, text);

            if (size < ring.Length)
            {
                ring[(head + size) % ring.Length] = entry;
                size++;
            }
            else
            {
                ring[head] = entry;
                head = (head + 1) % ring.Length;
            }

            return latestSeq;
        }
    }

    /// <summary>
    /// 返回 seq &gt; <paramref name="since"/> 的最多 <paramref name="limit"/> 条日志。
    /// since 早于缓冲最旧 seq 时从最旧返回并给出 <c>dropped</c>（丢失计数）。
    /// </summary>
    internal LogEntryPage Snapshot(long since, int limit)
    {
        lock (gate)
        {
            if (limit <= 0)
            {
                limit = DefaultLimit;
            }

            if (limit > ring.Length)
            {
                limit = ring.Length;
            }

            var effectiveSince = since < 0 ? 0 : since;

            var dropped = 0;
            var oldestSeq = size > 0 ? ring[head].Seq : latestSeq + 1;
            if (size > 0 && effectiveSince < oldestSeq - 1)
            {
                var lost = oldestSeq - 1 - effectiveSince;
                dropped = lost > int.MaxValue ? int.MaxValue : (int)lost;
                effectiveSince = oldestSeq - 1;
            }

            var startOffset = 0;
            while (startOffset < size && ring[(head + startOffset) % ring.Length].Seq <= effectiveSince)
            {
                startOffset++;
            }

            var available = size - startOffset;
            if (available > limit)
            {
                available = limit;
            }

            var entries = new LogEntry[available];
            for (var i = 0; i < available; i++)
            {
                entries[i] = ring[(head + startOffset + i) % ring.Length];
            }

            return new LogEntryPage(latestSeq, dropped, entries);
        }
    }
}

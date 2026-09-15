using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 增量拉取结果：<see cref="Seq"/> 为桥进程内最新 seq（客户端据此判断是否还有新事件），
/// <see cref="Dropped"/> 为因 since 过旧而丢失的事件数，<see cref="Events"/> 为本次返回的增量。
/// </summary>
internal readonly struct RaidEventPage
{
    internal RaidEventPage(long seq, int dropped, RaidEvent[] events)
    {
        Seq = seq;
        Dropped = dropped;
        Events = events;
    }

    internal long Seq { get; }

    internal int Dropped { get; }

    internal RaidEvent[] Events { get; }
}

/// <summary>
/// 线程安全环形事件缓冲（容量默认 1000）：Unity 主线程 / 游戏事件回调写，
/// HTTP 线程读。seq 桥进程内单调递增；缓冲跨 raid 保留（事件自带 raidId），
/// 超出容量后自然淘汰最旧事件。
/// </summary>
internal sealed class RaidEventBuffer
{
    internal const int DefaultCapacity = 1000;

    /// <summary>未指定 limit 时的默认返回条数。</summary>
    internal const int DefaultLimit = 100;

    private readonly object gate = new object();
    private readonly RaidEvent[] ring;

    /// <summary>最旧事件的环下标。</summary>
    private int head;

    private int size;

    /// <summary>已分配的最新 seq；0 表示尚无事件。</summary>
    private long latestSeq;

    internal RaidEventBuffer(int capacity = DefaultCapacity)
    {
        if (capacity < 1)
        {
            capacity = 1;
        }

        ring = new RaidEvent[capacity];
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

    internal long AppendDamage(long timestampUtcTicks, string raidId, in DamagePayload payload)
    {
        return Append(timestampUtcTicks, RaidEventKind.Damage, raidId, payload, default, default);
    }

    internal long AppendDeath(long timestampUtcTicks, string raidId, in DeathPayload payload)
    {
        return Append(timestampUtcTicks, RaidEventKind.Death, raidId, default, payload, default);
    }

    internal long AppendExtraction(long timestampUtcTicks, string raidId, in ExtractionPayload payload)
    {
        return Append(timestampUtcTicks, RaidEventKind.Extraction, raidId, default, default, payload);
    }

    /// <summary>
    /// 返回 seq &gt; <paramref name="since"/> 的最多 <paramref name="limit"/> 条事件。
    /// since 早于缓冲最旧 seq 时从最旧返回并给出 <c>dropped</c>（丢失计数）。
    /// </summary>
    internal RaidEventPage Snapshot(long since, int limit)
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

            var events = new RaidEvent[available];
            for (var i = 0; i < available; i++)
            {
                events[i] = ring[(head + startOffset + i) % ring.Length];
            }

            return new RaidEventPage(latestSeq, dropped, events);
        }
    }

    private long Append(
        long timestampUtcTicks,
        RaidEventKind kind,
        string raidId,
        DamagePayload damage,
        DeathPayload death,
        ExtractionPayload extraction)
    {
        lock (gate)
        {
            latestSeq++;
            var raidEvent = new RaidEvent(
                latestSeq,
                timestampUtcTicks,
                kind,
                raidId ?? string.Empty,
                damage,
                death,
                extraction);

            if (size < ring.Length)
            {
                ring[(head + size) % ring.Length] = raidEvent;
                size++;
            }
            else
            {
                ring[head] = raidEvent;
                head = (head + 1) % ring.Length;
            }

            return latestSeq;
        }
    }
}

using System;
using System.Collections.Generic;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 聚合组（不可变快照）。<see cref="Key"/> 为归一化文本（见 <see cref="LogSummaryNormalizer"/>），
/// <see cref="Level"/> 为该组观测到的最高严重度，<see cref="Source"/> 为首个来源，
/// <see cref="Count"/> 为**全部**观测条数（不受 /logs/recent 环形缓冲容量影响），
/// <see cref="SampleText"/> 为首个原始文本样本。
/// </summary>
internal sealed class LogSummaryGroup
{
    internal LogSummaryGroup(
        string key,
        string level,
        string source,
        long count,
        long firstTimestampUtcTicks,
        long lastTimestampUtcTicks,
        string sampleText)
    {
        Key = key;
        Level = level;
        Source = source;
        Count = count;
        FirstTimestampUtcTicks = firstTimestampUtcTicks;
        LastTimestampUtcTicks = lastTimestampUtcTicks;
        SampleText = sampleText;
    }

    internal string Key { get; }

    internal string Level { get; }

    internal string Source { get; }

    internal long Count { get; }

    internal long FirstTimestampUtcTicks { get; }

    internal long LastTimestampUtcTicks { get; }

    internal string SampleText { get; }
}

/// <summary>
/// 聚合读取结果：<see cref="Groups"/> 已按「count 降序 → lastTs 降序 → key 序升序」排序，
/// <see cref="OverflowDropped"/> 为累计被淘汰的组数（进程内单调递增，不随 since 重置）。
/// </summary>
internal readonly struct LogSummaryPage
{
    internal LogSummaryPage(LogSummaryGroup[] groups, int overflowDropped)
    {
        Groups = groups;
        OverflowDropped = overflowDropped;
    }

    internal LogSummaryGroup[] Groups { get; }

    internal int OverflowDropped { get; }
}

/// <summary>
/// 日志归一化聚合存储（纯内存，可独立单测）：与 <see cref="LogRingBuffer"/> **并列**的写路径，
/// 消费每一条被采集的日志条目。与环形缓冲的关键区别：计数不随缓冲淘汰丢失——
/// 数千条刷屏错误始终显示完整 <c>count</c>（只有聚合组本身受 500 组上限约束）。
///
/// 线程安全：日志回调线程写（<see cref="Observe"/>）vs HTTP 线程读（<see cref="Snapshot"/>），
/// 全部经同一把 <c>gate</c> 锁。**绝不与 <see cref="LogRingBuffer"/> 的锁嵌套**
/// （两条写路径彼此独立、顺序无关），故不存在锁顺序倒置。
///
/// 溢出策略：组数达 <c>maxGroups</c>（默认 <see cref="DefaultMaxGroups"/>）后再遇新键 →
/// 淘汰 <c>lastTs</c> 最小（最久未更新）的组，<c>lastTs</c> 并列时淘汰 key 序最小者（确定性）；
/// 每次淘汰令累计溢出计数加一（经 <see cref="LogSummaryPage.OverflowDropped"/> 暴露）。
/// </summary>
internal sealed class LogSummaryStore
{
    internal const int DefaultMaxGroups = 500;

    private readonly object gate = new object();
    private readonly Dictionary<string, GroupState> groups =
        new Dictionary<string, GroupState>(StringComparer.Ordinal);

    private readonly int maxGroups;
    private int overflowDropped;

    internal LogSummaryStore(int maxGroups = DefaultMaxGroups)
    {
        this.maxGroups = maxGroups < 1 ? 1 : maxGroups;
    }

    /// <summary>
    /// 观测一条已采集日志（只写内存、零 I/O）。级别与来源为 null 时按空串处理。
    /// </summary>
    internal void Observe(long timestampUtcTicks, string level, string source, string text)
    {
        var key = LogSummaryNormalizer.Normalize(text);
        var levelName = LogWatchLevel.Normalize(level);
        var sourceName = source ?? string.Empty;

        lock (gate)
        {
            if (groups.TryGetValue(key, out var existing))
            {
                existing.Observe(timestampUtcTicks, levelName);
                return;
            }

            if (groups.Count >= maxGroups)
            {
                EvictLeastRecentlyUpdated();
            }

            groups[key] = new GroupState(key, levelName, sourceName, timestampUtcTicks, text ?? string.Empty);
        }
    }

    /// <summary>
    /// 返回 <c>lastTs &gt; sinceUtcTicks</c>（since 独占）的组快照；
    /// <paramref name="sinceUtcTicks"/> ≤ 0 表示不过滤。返回的是不可变副本，
    /// 调用方持有期间后续写入不会改变它。
    /// </summary>
    internal LogSummaryPage Snapshot(long sinceUtcTicks)
    {
        lock (gate)
        {
            var effectiveSince = sinceUtcTicks < 0 ? 0 : sinceUtcTicks;
            var snapshot = new List<LogSummaryGroup>(groups.Count);

            foreach (var pair in groups)
            {
                var state = pair.Value;
                if (state.LastTimestampUtcTicks <= effectiveSince)
                {
                    continue;
                }

                snapshot.Add(new LogSummaryGroup(
                    state.Key,
                    state.Level,
                    state.Source,
                    state.Count,
                    state.FirstTimestampUtcTicks,
                    state.LastTimestampUtcTicks,
                    state.SampleText));
            }

            snapshot.Sort(CompareGroups);
            return new LogSummaryPage(snapshot.ToArray(), overflowDropped);
        }
    }

    /// <summary>溢出淘汰：最久未更新（lastTs 最小）优先；并列取 key 序最小者（确定性）。</summary>
    private void EvictLeastRecentlyUpdated()
    {
        string victimKey = null;
        var victimLastTicks = long.MaxValue;

        foreach (var pair in groups)
        {
            var state = pair.Value;
            if (victimKey == null
                || state.LastTimestampUtcTicks < victimLastTicks
                || (state.LastTimestampUtcTicks == victimLastTicks
                    && string.CompareOrdinal(state.Key, victimKey) < 0))
            {
                victimKey = state.Key;
                victimLastTicks = state.LastTimestampUtcTicks;
            }
        }

        if (victimKey != null)
        {
            groups.Remove(victimKey);
            overflowDropped++;
        }
    }

    private static int CompareGroups(LogSummaryGroup left, LogSummaryGroup right)
    {
        if (left.Count != right.Count)
        {
            return left.Count > right.Count ? -1 : 1;
        }

        if (left.LastTimestampUtcTicks != right.LastTimestampUtcTicks)
        {
            return left.LastTimestampUtcTicks > right.LastTimestampUtcTicks ? -1 : 1;
        }

        return string.CompareOrdinal(left.Key, right.Key);
    }

    /// <summary>组内可变状态（仅在 <c>gate</c> 锁内读写）。</summary>
    private sealed class GroupState
    {
        internal GroupState(string key, string level, string source, long ticks, string sampleText)
        {
            Key = key;
            Level = level;
            Source = source;
            Count = 1;
            FirstTimestampUtcTicks = ticks;
            LastTimestampUtcTicks = ticks;
            SampleText = sampleText;
        }

        internal string Key { get; }

        internal string Level { get; set; }

        internal string Source { get; }

        internal long Count { get; private set; }

        internal long FirstTimestampUtcTicks { get; private set; }

        internal long LastTimestampUtcTicks { get; private set; }

        internal string SampleText { get; }

        internal void Observe(long ticks, string level)
        {
            Count++;

            if (ticks < FirstTimestampUtcTicks)
            {
                FirstTimestampUtcTicks = ticks;
            }

            if (ticks > LastTimestampUtcTicks)
            {
                LastTimestampUtcTicks = ticks;
            }

            // 升级为观测到的最高严重度：组内出现 Error 时不得仍标 Warning。
            if (LogWatchLevel.Rank(level) < LogWatchLevel.Rank(Level))
            {
                Level = level;
            }
        }
    }
}

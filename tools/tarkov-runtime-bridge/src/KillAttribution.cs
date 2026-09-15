using System;
using System.Collections.Generic;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 击杀归属映射（纯逻辑）：由受伤事件维护 <c>victimProfileId → lastDamager</c>，
/// 死亡时在时间窗内取出并消费（每个 victim 只归属一次）。
/// 字典键为 victimProfileId；每条记录另存 raidId，跨 raid 的陈旧记录不参与归属。
/// 不可得时返回 false（不猜）。
/// </summary>
internal sealed class KillAttribution
{
    /// <summary>归属时间窗：最后一次受伤到死亡超过该毫秒数则不归属。</summary>
    internal const long DefaultWindowMs = 10_000;

    private readonly long windowMs;
    private readonly Dictionary<string, Record> records = new Dictionary<string, Record>(StringComparer.Ordinal);

    internal KillAttribution(long windowMs = DefaultWindowMs)
    {
        this.windowMs = windowMs < 0 ? 0 : windowMs;
    }

    /// <summary>记录一次受伤来源（victim 为空串时忽略）。</summary>
    internal void RecordDamage(string raidId, string victimProfileId, long nowMs, in KillerInfo attacker)
    {
        if (string.IsNullOrEmpty(victimProfileId))
        {
            return;
        }

        lock (records)
        {
            records[victimProfileId] = new Record(raidId ?? string.Empty, nowMs, attacker);
        }
    }

    /// <summary>
    /// 取出 victim 在窗口内的最后攻击者。命中即消费（重复调用返回 false）。
    /// raidId 不匹配或超窗视为不可得。
    /// </summary>
    internal bool TryTakeKiller(string raidId, string victimProfileId, long nowMs, out KillerInfo killer)
    {
        killer = default;
        if (string.IsNullOrEmpty(victimProfileId))
        {
            return false;
        }

        lock (records)
        {
            if (!records.TryGetValue(victimProfileId, out var record))
            {
                return false;
            }

            records.Remove(victimProfileId);

            if (!string.Equals(record.RaidId, raidId ?? string.Empty, StringComparison.Ordinal))
            {
                return false;
            }

            if (nowMs - record.RaidMs > windowMs)
            {
                return false;
            }

            killer = record.Killer;
            return true;
        }
    }

    /// <summary>raid 结束时清空，避免跨 raid 泄漏。</summary>
    internal void Clear()
    {
        lock (records)
        {
            records.Clear();
        }
    }

    private readonly struct Record
    {
        internal Record(string raidId, long raidMs, KillerInfo killer)
        {
            RaidId = raidId;
            RaidMs = raidMs;
            Killer = killer;
        }

        internal string RaidId { get; }

        internal long RaidMs { get; }

        internal KillerInfo Killer { get; }
    }
}

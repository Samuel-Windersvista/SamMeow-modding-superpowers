using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// raid 事件类型（JSON <c>type</c> 字段的稳定取值）。
/// 击杀不单列事件：击杀 = <see cref="Death"/> 且 payload.killer != null。
/// </summary>
internal enum RaidEventKind
{
    Damage,
    Death,
    Extraction,
}

/// <summary>击杀归属（<c>death.payload.killer</c>）；不可得时整体为 JSON <c>null</c>。</summary>
internal readonly struct KillerInfo
{
    internal KillerInfo(string profileId, string name, string side, string role, bool isLocal)
    {
        ProfileId = profileId;
        Name = name;
        Side = side;
        Role = role;
        IsLocal = isLocal;
    }

    internal string ProfileId { get; }

    internal string Name { get; }

    internal string Side { get; }

    internal string Role { get; }

    internal bool IsLocal { get; }
}

/// <summary><c>damage</c> 事件负载。</summary>
internal readonly struct DamagePayload
{
    internal DamagePayload(string victimProfileId, bool victimIsLocal, string part, float amount, string sourceType)
    {
        VictimProfileId = victimProfileId;
        VictimIsLocal = victimIsLocal;
        Part = part;
        Amount = amount;
        SourceType = sourceType;
    }

    internal string VictimProfileId { get; }

    internal bool VictimIsLocal { get; }

    /// <summary><c>EBodyPart.ToString()</c>，如 Head / Chest。</summary>
    internal string Part { get; }

    internal float Amount { get; }

    /// <summary><c>EDamageType.ToString()</c>（DamageInfo.DamageType）。</summary>
    internal string SourceType { get; }
}

/// <summary><c>death</c> 事件负载；<see cref="HasKiller"/> 为假时 killer 输出 JSON <c>null</c>。</summary>
internal readonly struct DeathPayload
{
    internal DeathPayload(
        string victimProfileId,
        bool victimIsLocal,
        string damageType,
        bool hasKiller,
        KillerInfo killer)
    {
        VictimProfileId = victimProfileId;
        VictimIsLocal = victimIsLocal;
        DamageType = damageType;
        HasKiller = hasKiller;
        Killer = killer;
    }

    internal string VictimProfileId { get; }

    internal bool VictimIsLocal { get; }

    /// <summary><c>EDamageType.ToString()</c>（DiedEvent 参数）。</summary>
    internal string DamageType { get; }

    internal bool HasKiller { get; }

    internal KillerInfo Killer { get; }
}

/// <summary><c>extraction</c> 事件负载（本地玩家）。</summary>
internal readonly struct ExtractionPayload
{
    internal ExtractionPayload(string exitName, string status)
    {
        ExitName = exitName;
        Status = status;
    }

    /// <summary>撤离点名（<c>LocalGame.Stop</c> 的 exitName）。</summary>
    internal string ExitName { get; }

    /// <summary><c>ExitStatus.ToString()</c>：Survived / Killed / Left / Runner / MissingInAction / Transit。</summary>
    internal string Status { get; }
}

/// <summary>
/// 单条 raid 事件（不可变）。<see cref="Seq"/> 由 <see cref="RaidEventBuffer"/> 单调分配，
/// <see cref="TimestampUtcTicks"/> 为事件发生时的墙钟 UTC Ticks。
/// </summary>
internal sealed class RaidEvent
{
    internal RaidEvent(
        long seq,
        long timestampUtcTicks,
        RaidEventKind kind,
        string raidId,
        DamagePayload damage,
        DeathPayload death,
        ExtractionPayload extraction)
    {
        Seq = seq;
        TimestampUtcTicks = timestampUtcTicks;
        Kind = kind;
        RaidId = raidId;
        Damage = damage;
        Death = death;
        Extraction = extraction;
    }

    internal long Seq { get; }

    internal long TimestampUtcTicks { get; }

    internal RaidEventKind Kind { get; }

    /// <summary>事件所属 raid（跨 raid 保留时用于区分）。</summary>
    internal string RaidId { get; }

    internal DamagePayload Damage { get; }

    internal DeathPayload Death { get; }

    internal ExtractionPayload Extraction { get; }

    /// <summary>JSON <c>type</c> 字段的稳定字面量。</summary>
    internal static string TypeName(RaidEventKind kind)
    {
        switch (kind)
        {
            case RaidEventKind.Damage:
                return "damage";
            case RaidEventKind.Death:
                return "death";
            case RaidEventKind.Extraction:
                return "extraction";
            default:
                // 未知 kind 不得伪装成 extraction：显式 "unknown"，便于发现枚举 / 序列化漂移。
                return "unknown";
        }
    }
}

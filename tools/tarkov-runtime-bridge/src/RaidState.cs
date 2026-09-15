using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>三维坐标快照（Unity 主线程读，HTTP 线程只读）。</summary>
internal readonly struct Vector3Snapshot
{
    internal Vector3Snapshot(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    internal float X { get; }

    internal float Y { get; }

    internal float Z { get; }
}

/// <summary>二维向量快照，对应 <c>Player.Rotation</c>（水平朝向）。</summary>
internal readonly struct Vector2Snapshot
{
    internal Vector2Snapshot(float x, float y)
    {
        X = x;
        Y = y;
    }

    internal float X { get; }

    internal float Y { get; }
}

/// <summary>
/// 玩家血量快照。<c>Total</c> 取自 <c>EBodyPart.Common</c>（native 聚合分支），
/// 各肢体取 <c>GetBodyPartHealth(&lt;EBodyPart&gt;).Current</c>。
/// </summary>
internal readonly struct HealthSnapshot
{
    internal HealthSnapshot(
        bool alive,
        float total,
        float head,
        float chest,
        float stomach,
        float leftArm,
        float rightArm,
        float leftLeg,
        float rightLeg)
    {
        Alive = alive;
        Total = total;
        Head = head;
        Chest = chest;
        Stomach = stomach;
        LeftArm = leftArm;
        RightArm = rightArm;
        LeftLeg = leftLeg;
        RightLeg = rightLeg;
    }

    internal bool Alive { get; }

    internal float Total { get; }

    internal float Head { get; }

    internal float Chest { get; }

    internal float Stomach { get; }

    internal float LeftArm { get; }

    internal float RightArm { get; }

    internal float LeftLeg { get; }

    internal float RightLeg { get; }
}

/// <summary>
/// 当前手持武器快照（仅持枪时非 null；近战 / 投掷 / 空手为 null）。
/// 源：<c>Player.HandsController</c> → <c>IFirearmHandsController.Item</c>。
/// </summary>
internal sealed class WeaponSnapshot
{
    internal WeaponSnapshot(string templateId, string name, int ammoInMag, int ammoInChamber)
    {
        TemplateId = templateId;
        Name = name;
        AmmoInMag = ammoInMag;
        AmmoInChamber = ammoInChamber;
    }

    /// <summary>tpl：<c>Item.StringTemplateId</c>（回退 <c>TemplateId.ToString()</c>）。</summary>
    internal string TemplateId { get; }

    /// <summary>展示名（<c>Item.Name</c>，回退 <c>ShortName</c>）。</summary>
    internal string Name { get; }

    /// <summary>弹匣内弹药数（<c>Weapon.GetCurrentMagazineCount()</c>）。</summary>
    internal int AmmoInMag { get; }

    /// <summary>枪膛内弹药数（<c>Weapon.ChamberAmmoCount</c>）。</summary>
    internal int AmmoInChamber { get; }
}

/// <summary>单个已装备槽摘要（空槽不产出条目）。</summary>
internal readonly struct EquipmentEntry
{
    internal EquipmentEntry(string slot, string templateId, string name)
    {
        Slot = slot;
        TemplateId = templateId;
        Name = name;
    }

    /// <summary>槽名（<c>Slot.Name</c>），如 Headwear / ArmorVest / Backpack。</summary>
    internal string Slot { get; }

    internal string TemplateId { get; }

    internal string Name { get; }
}

/// <summary>玩家域快照：位置 / 水平朝向 / 姿态 / 血量 / 当前武器 / 已装备槽。</summary>
internal readonly struct PlayerSnapshot
{
    internal PlayerSnapshot(
        Vector3Snapshot position,
        Vector2Snapshot rotation,
        string pose,
        HealthSnapshot health,
        WeaponSnapshot weapon,
        EquipmentEntry[] equipment)
    {
        Position = position;
        Rotation = rotation;
        Pose = pose;
        Health = health;
        Weapon = weapon;
        Equipment = equipment;
    }

    internal Vector3Snapshot Position { get; }

    internal Vector2Snapshot Rotation { get; }

    /// <summary><c>EPlayerPose.ToString()</c>：Prone / Duck / Stand。</summary>
    internal string Pose { get; }

    internal HealthSnapshot Health { get; }

    /// <summary>当前手持武器；非持枪为 null。</summary>
    internal WeaponSnapshot Weapon { get; }

    /// <summary>已装备槽摘要（仅含已占用槽；无装备时为空数组）。</summary>
    internal EquipmentEntry[] Equipment { get; }
}

/// <summary>raid 元数据快照：地图 / 状态 / 剩余时间 / 组合 raidId。</summary>
internal readonly struct RaidMetaSnapshot
{
    internal RaidMetaSnapshot(string map, string status, float remainingSeconds, string raidId)
    {
        Map = map;
        Status = status;
        RemainingSeconds = remainingSeconds;
        RaidId = raidId;
    }

    internal string Map { get; }

    internal string Status { get; }

    internal float RemainingSeconds { get; }

    /// <summary>确定性组合：<c>&lt;profileId&gt;@&lt;桥自持会话起点 UTC ISO&gt;</c>（缺失时 <c>no-start</c>）。</summary>
    internal string RaidId { get; }
}

/// <summary>单个 bot 明细条目（位置 / 角色 / 阵营 / 存活）。</summary>
internal readonly struct BotEntry
{
    internal BotEntry(Vector3Snapshot position, string role, string side, bool alive)
    {
        Position = position;
        Role = role;
        Side = side;
        Alive = alive;
    }

    internal Vector3Snapshot Position { get; }

    /// <summary><c>WildSpawnType.ToString()</c>，如 bossKilla / pmcBot / assault。</summary>
    internal string Role { get; }

    /// <summary><c>EPlayerSide.ToString()</c>：Usec / Bear / Savage。</summary>
    internal string Side { get; }

    internal bool Alive { get; }
}

/// <summary>bot 摘要计数与生成器计数。</summary>
internal readonly struct BotSummary
{
    internal BotSummary(
        int total,
        int alive,
        int pmc,
        int scav,
        int boss,
        int other,
        int spawnerAliveAndLoading,
        int spawnerDelayed,
        int spawnerAllWithDelayed)
    {
        Total = total;
        Alive = alive;
        Pmc = pmc;
        Scav = scav;
        Boss = boss;
        Other = other;
        SpawnerAliveAndLoading = spawnerAliveAndLoading;
        SpawnerDelayed = spawnerDelayed;
        SpawnerAllWithDelayed = spawnerAllWithDelayed;
    }

    internal int Total { get; }

    internal int Alive { get; }

    internal int Pmc { get; }

    internal int Scav { get; }

    internal int Boss { get; }

    internal int Other { get; }

    internal int SpawnerAliveAndLoading { get; }

    internal int SpawnerDelayed { get; }

    internal int SpawnerAllWithDelayed { get; }
}

/// <summary>
/// 一次完整采样（不可变）。Unity 主线程构造并发布，HTTP 线程只读。
/// 存在即代表「在 raid」（GameWorld 与 MainPlayer 均非空）。
/// </summary>
internal sealed class RaidState
{
    internal RaidState(
        PlayerSnapshot player,
        RaidMetaSnapshot raid,
        BotSummary bots,
        BotEntry[] botDetails,
        long sampledAtMs)
    {
        Player = player;
        Raid = raid;
        Bots = bots;
        BotDetails = botDetails;
        SampledAtMs = sampledAtMs;
    }

    internal PlayerSnapshot Player { get; }

    internal RaidMetaSnapshot Raid { get; }

    internal BotSummary Bots { get; }

    /// <summary>bot 明细（最多 <c>BotDetailLimit</c> 条；截断标志由调用方按 Total 比较得出）。</summary>
    internal BotEntry[] BotDetails { get; }

    /// <summary>采样时刻（单调毫秒时钟，Environment.TickCount64）。</summary>
    internal long SampledAtMs { get; }
}

/// <summary>
/// 线程安全快照存储：Unity 主线程（<see cref="PositionSampler"/>）写，
/// HTTP 线程（<see cref="HttpBridgeServer"/>）读。<c>null</c> 表示当前不在 raid。
/// </summary>
internal sealed class RaidStateStore
{
    private readonly object gate = new object();
    private RaidState current;

    internal void Publish(RaidState state)
    {
        lock (gate)
        {
            current = state;
        }
    }

    internal void Clear()
    {
        lock (gate)
        {
            current = null;
        }
    }

    internal bool TryGet(out RaidState state)
    {
        lock (gate)
        {
            state = current;
            return state != null;
        }
    }
}

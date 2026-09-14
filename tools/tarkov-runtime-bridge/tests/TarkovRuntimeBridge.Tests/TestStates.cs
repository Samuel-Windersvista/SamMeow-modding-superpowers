using System;

namespace SamMeow.TarkovRuntimeBridge.Tests;

/// <summary>构造纯逻辑测试所需的快照（不触碰任何游戏程序集）。</summary>
internal static class TestStates
{
    /// <summary>默认玩家快照：位置 (1,2,3)、朝向 (0,90)、Stand、满血。</summary>
    internal static RaidState PlayerState(
        float x = 1f,
        float y = 2f,
        float z = 3f,
        float rotationX = 0f,
        float rotationY = 90f,
        string pose = "Stand",
        bool alive = true,
        float total = 100f,
        long sampledAtMs = 1000)
    {
        return new RaidState(
            new PlayerSnapshot(
                new Vector3Snapshot(x, y, z),
                new Vector2Snapshot(rotationX, rotationY),
                pose,
                new HealthSnapshot(alive, total, 35f, 40f, 30f, 25f, 25f, 30f, 30f)),
            new RaidMetaSnapshot(
                "factory4_day",
                "Started",
                120.5f,
                "profile-1@2026-09-14T00:00:00.0000000Z"),
            new BotSummary(4, 4, 2, 1, 1, 0, 3, 1, 4),
            Array.Empty<BotEntry>(),
            sampledAtMs);
    }

    /// <summary>带 bot 明细的快照；<paramref name="total"/> 大于明细数时响应应标 truncated。</summary>
    internal static RaidState BotsState(int total, params BotEntry[] details)
    {
        return new RaidState(
            new PlayerSnapshot(
                new Vector3Snapshot(1f, 2f, 3f),
                new Vector2Snapshot(0f, 90f),
                "Stand",
                new HealthSnapshot(true, 100f, 35f, 40f, 30f, 25f, 25f, 30f, 30f)),
            new RaidMetaSnapshot("factory4_day", "Started", 120.5f, "profile-1@2026-09-14T00:00:00.0000000Z"),
            new BotSummary(total, total, 2, 1, 1, 0, 3, 1, 4),
            details,
            1000);
    }
}

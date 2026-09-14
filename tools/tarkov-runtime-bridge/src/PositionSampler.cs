using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 主线程采样器：按配置间隔读取玩家域（位置 / 朝向 / 姿态 / 血量）、
/// raid 元数据（地图 / 状态 / 剩余时间 / raidId）与 bot 域（存活列表 + 生成器计数），
/// 组装为一次 <see cref="RaidState"/> 写入 <see cref="RaidStateStore"/>。
/// 全部读取在同一主线程循环内完成；异常仅记日志，不向 Unity 抛出，并保留上一次快照。
/// MainPlayer 为空（未进 raid / 已撤离）时清除快照，标记「不在 raid」。
/// </summary>
// STD-LOG-003：客户端日志统一走 BepInEx 日志源（此处为注入的 ManualLogSource）。
public sealed class PositionSampler : MonoBehaviour
{
    /// <summary>bot 明细上限（响应体积保护）。</summary>
    internal const int BotDetailLimit = 200;

    private RaidStateStore store;
    private ManualLogSource log;
    private float intervalSeconds = 1f;
    private float elapsed;
    private bool initialized;

    /// <summary>
    /// 当前 raid 会话起点的桥进程墙钟 UTC（Ticks）；0 表示无活动会话。
    /// MainPlayer 从无到有时记录（含桥在 raid 中途启动的首次观测），离开 raid 时清零。
    /// 不用 GameTimer.StartDateTime（live 实测不可靠）。
    /// </summary>
    private long sessionStartUtcTicks;

    public PositionSampler(IntPtr pointer) : base(pointer)
    {
    }

    /// <summary>
    /// 由 <see cref="Plugin"/> 在挂载后注入依赖。
    /// 含托管类型参数，须标 [HideFromIl2Cpp]，避免 il2cpp 尝试封送。
    /// </summary>
    [HideFromIl2Cpp]
    internal void Initialize(RaidStateStore stateStore, int sampleIntervalMs, ManualLogSource logger)
    {
        store = stateStore;
        log = logger;
        intervalSeconds = Math.Max(0.05f, sampleIntervalMs / 1000f);
        // 首帧立即采样一次，避免进 raid 后等待一个完整间隔。
        elapsed = intervalSeconds;
        initialized = true;
    }

    public void Update()
    {
        if (!initialized)
        {
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        if (elapsed < intervalSeconds)
        {
            return;
        }

        elapsed = 0f;
        Sample();
    }

    [HideFromIl2Cpp]
    private void Sample()
    {
        try
        {
            var world = Singleton<GameWorld>.Instance;
            if (world == null)
            {
                sessionStartUtcTicks = 0;
                store.Clear();
                return;
            }

            var player = world.MainPlayer;
            if (player == null)
            {
                sessionStartUtcTicks = 0;
                store.Clear();
                return;
            }

            // 桥自持会话起点：MainPlayer 首次出现（含桥在 raid 中途启动）时记录墙钟 UTC。
            if (sessionStartUtcTicks == 0)
            {
                sessionStartUtcTicks = DateTime.UtcNow.Ticks;
            }

            var playerSnapshot = SamplePlayer(player);
            var raidMeta = SampleRaid(world, player, sessionStartUtcTicks);
            var bots = SampleBots(world, out var botDetails);

            store.Publish(new RaidState(
                playerSnapshot,
                raidMeta,
                bots,
                botDetails,
                Environment.TickCount64));
        }
        catch (Exception exception)
        {
            // 读取失败不得打断主线程；保留上一次快照，仅记录异常。
            log.LogWarning($"Raid sample failed: {exception.Message}");
        }
    }

    [HideFromIl2Cpp]
    private static PlayerSnapshot SamplePlayer(Player player)
    {
        var position = player.Position;
        var rotation = player.Rotation;
        var pose = player.Pose.ToString();

        var alive = false;
        var total = 0f;
        var head = 0f;
        var chest = 0f;
        var stomach = 0f;
        var leftArm = 0f;
        var rightArm = 0f;
        var leftLeg = 0f;
        var rightLeg = 0f;

        var healthController = player.ActiveHealthController;
        if (healthController != null)
        {
            alive = healthController.IsAlive;
            total = healthController.GetBodyPartHealth(EBodyPart.Common).Current;
            head = healthController.GetBodyPartHealth(EBodyPart.Head).Current;
            chest = healthController.GetBodyPartHealth(EBodyPart.Chest).Current;
            stomach = healthController.GetBodyPartHealth(EBodyPart.Stomach).Current;
            leftArm = healthController.GetBodyPartHealth(EBodyPart.LeftArm).Current;
            rightArm = healthController.GetBodyPartHealth(EBodyPart.RightArm).Current;
            leftLeg = healthController.GetBodyPartHealth(EBodyPart.LeftLeg).Current;
            rightLeg = healthController.GetBodyPartHealth(EBodyPart.RightLeg).Current;
        }

        return new PlayerSnapshot(
            new Vector3Snapshot(position.x, position.y, position.z),
            new Vector2Snapshot(rotation.x, rotation.y),
            pose,
            new HealthSnapshot(alive, total, head, chest, stomach, leftArm, rightArm, leftLeg, rightLeg));
    }

    [HideFromIl2Cpp]
    private static RaidMetaSnapshot SampleRaid(GameWorld world, Player player, long sessionStartUtcTicks)
    {
        // profileId 回退链（纯逻辑见 RaidIdBuilder）：MainPlayer.ProfileId
        // （live 实测 GameWorld.CurrentProfileId 在客户端为空）→ CurrentProfileId → unknown-profile。
        // player 由 Sample() 单次取值传入，避免重复访问 world.MainPlayer（评审 smell 修正）。
        var profileId = RaidIdBuilder.ResolveProfileId(
            player.ProfileId,
            world.CurrentProfileId.HasValue ? world.CurrentProfileId.Value.ToString() : null);

        // 会话起点由桥自持（见 Sample），不再读取不可靠的 GameTimer.StartDateTime。
        var raidId = RaidIdBuilder.Build(profileId, sessionStartUtcTicks);

        var game = Singleton<AbstractGame>.Instance;
        if (game == null)
        {
            return new RaidMetaSnapshot(string.Empty, string.Empty, 0f, raidId);
        }

        var map = game.LocationId ?? string.Empty;
        var status = game.Status.ToString();

        var remainingSeconds = 0f;
        var timer = game.GameTimer;
        if (timer != null)
        {
            remainingSeconds = timer.EscapeTimeSeconds();
        }

        return new RaidMetaSnapshot(map, status, remainingSeconds, raidId);
    }

    [HideFromIl2Cpp]
    private static BotSummary SampleBots(GameWorld world, out BotEntry[] botDetails)
    {
        var total = 0;
        var pmc = 0;
        var scav = 0;
        var boss = 0;
        var other = 0;
        var collected = new List<BotEntry>(16);

        var players = world.AllAlivePlayersList;
        if (players != null)
        {
            foreach (var bot in players)
            {
                if (bot == null || bot.IsYourPlayer)
                {
                    continue;
                }

                total++;

                var side = EPlayerSide.Savage;
                var role = string.Empty;

                var profile = bot.Profile;
                if (profile != null)
                {
                    side = profile.Side;

                    var info = profile.Info;
                    if (info != null)
                    {
                        var settings = info.Settings;
                        if (settings != null)
                        {
                            role = settings.Role.ToString();
                        }
                    }
                }

                // 分类优先级（role 优先）：boss（role 含 "boss"）> pmc（role 以 "pmc" 开头）
                // > scav（Savage）> other。SPT 的 pmcUSEC/pmcBEAR 阵营为 Savage，
                // 仅凭 side 会把 PMC 误判为 scav（live 实测），故先看 role。
                if (role.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    boss++;
                }
                else if (role.StartsWith("pmc", StringComparison.OrdinalIgnoreCase))
                {
                    pmc++;
                }
                else if (side == EPlayerSide.Savage)
                {
                    scav++;
                }
                else
                {
                    other++;
                }

                if (collected.Count < BotDetailLimit)
                {
                    var position = bot.Position;

                    var alive = false;
                    var healthController = bot.HealthController;
                    if (healthController != null)
                    {
                        alive = healthController.IsAlive;
                    }

                    collected.Add(new BotEntry(
                        new Vector3Snapshot(position.x, position.y, position.z),
                        role,
                        side.ToString(),
                        alive));
                }
            }
        }

        var spawnerAliveAndLoading = 0;
        var spawnerDelayed = 0;
        var spawnerAllWithDelayed = 0;

        var botGame = Singleton<IBotGame>.Instance;
        if (botGame != null)
        {
            var controller = botGame.BotsController;
            if (controller != null)
            {
                var spawner = controller.BotSpawner;
                if (spawner != null)
                {
                    spawnerAliveAndLoading = spawner.AliveAndLoadingBotsCount;
                    spawnerDelayed = spawner.BotsDelayed;
                    spawnerAllWithDelayed = spawner.AllBotsWithDelayed;
                }
            }
        }

        botDetails = collected.ToArray();

        // AllAlivePlayersList 本身只含存活角色，故 alive 与 total 同源同值。
        return new BotSummary(
            total,
            total,
            pmc,
            scav,
            boss,
            other,
            spawnerAliveAndLoading,
            spawnerDelayed,
            spawnerAllWithDelayed);
    }
}

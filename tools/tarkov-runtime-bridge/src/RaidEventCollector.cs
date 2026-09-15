using System;
using System.Collections.Generic;
using BepInEx.Logging;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using Il2CppInterop.Runtime;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// raid 事件采集器（游戏耦合层）：受伤经 Harmony patch
/// <c>ActiveHealthController.ApplyDamage</c>（<see cref="ApplyDamagePatch"/> prefix 记录归属 /
/// postfix 输出事件）采集；死亡订阅 <c>DiedEvent</c>；撤离由 <c>LocalGame.Stop</c> patch
/// （<see cref="LocalGameStopPatch"/>）回调 <see cref="NotifyLocalGameStop"/> 注入。
/// 受伤来源经 <see cref="KillAttribution"/> 产出击杀归属，事件写入线程安全
/// <see cref="RaidEventBuffer"/>。新玩家经 <c>GameWorld.OnPersonAdd</c> 挂死亡订阅；
/// 离开 raid 时解除订阅并清空归属，避免泄漏与跨 raid 串扰。
/// </summary>
// STD-LOG-003：日志走注入的 BepInEx ManualLogSource。
internal sealed class RaidEventCollector : IDisposable
{
    private readonly RaidEventBuffer buffer;
    private readonly ManualLogSource log;
    private readonly KillAttribution attribution = new KillAttribution();

    private readonly List<Subscription> subscriptions = new List<Subscription>();
    private readonly HashSet<IntPtr> subscribedControllers = new HashSet<IntPtr>();

    private GameWorld subscribedWorld;
    private Il2CppSystem.Action<IPlayer> personAddAction;
    private bool personAddSubscribed;
    private string currentRaidId = string.Empty;
    private string currentProfileId = string.Empty;

    /// <summary>是否处于 raid 采样中；受伤 Harmony 回调据此过滤菜单 / 藏身处的杂散调用。</summary>
    private bool inRaid;

    /// <summary>Harmony patch 的静态入口；未初始化时为 null（补丁空转）。</summary>
    internal static RaidEventCollector Current { get; private set; }

    internal RaidEventCollector(RaidEventBuffer buffer, ManualLogSource log)
    {
        this.buffer = buffer;
        this.log = log;
        Current = this;
    }

    /// <summary>
    /// 每次采样调用：缓存当前 raidId / profileId，绑定（或迁移）GameWorld 的 OnPersonAdd，
    /// 并为本地玩家与既有存活玩家补齐订阅（按健康控制器去重，幂等）。
    /// </summary>
    internal void Observe(GameWorld world, Player localPlayer, string raidId)
    {
        currentRaidId = raidId ?? string.Empty;
        currentProfileId = localPlayer != null ? (localPlayer.ProfileId ?? string.Empty) : string.Empty;
        inRaid = true;

        if (!ReferenceEquals(world, subscribedWorld))
        {
            DetachWorld();
            subscribedWorld = world;
        }

        // OnPersonAdd 订阅：失败时不置 personAddSubscribed，后续采样自动重试；
        // 委托引用保留，若 add 已部分生效，DetachWorld 仍能退订。
        if (world != null && !personAddSubscribed)
        {
            try
            {
                if (personAddAction == null)
                {
                    personAddAction = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<IPlayer>>(
                        new Action<IPlayer>(OnPersonAdd));
                }

                // 显式调用 native add 访问器（字段 += 会走 Delegate.Combine，语义不确定）。
                world.add_OnPersonAdd(personAddAction);
                personAddSubscribed = true;
            }
            catch (Exception exception)
            {
                log.LogWarning($"Bridge event collector: OnPersonAdd subscribe failed: {exception.Message}");
            }
        }

        Subscribe(localPlayer);

        var players = world != null ? world.AllAlivePlayersList : null;
        if (players != null)
        {
            foreach (var player in players)
            {
                Subscribe(player);
            }
        }
    }

    /// <summary>离开 raid：解除全部订阅并清空归属映射（幂等）。</summary>
    internal void LeaveRaid()
    {
        inRaid = false;

        if (subscribedWorld == null && subscriptions.Count == 0)
        {
            return;
        }

        // DetachWorld 内含 attribution.Clear()。
        DetachWorld();
    }

    /// <summary>Harmony patch 回调：本地游戏停止（撤离 / 死亡 / MIA）时注入 extraction 事件。</summary>
    internal static void NotifyLocalGameStop(string profileId, string exitStatus, string exitName)
    {
        Current?.HandleLocalGameStop(profileId, exitStatus, exitName);
    }

    /// <summary>Harmony prefix 回调：伤害结算前记录来源归属。</summary>
    internal static void NotifyDamageIncoming(ActiveHealthController controller, float amount, DamageInfo info)
    {
        Current?.HandleDamageIncoming(controller, amount, info);
    }

    /// <summary>Harmony postfix 回调：伤害结算后输出受伤事件。</summary>
    internal static void NotifyDamageApplied(
        ActiveHealthController controller, EBodyPart part, float amount, DamageInfo info)
    {
        Current?.HandleDamageApplied(controller, part, amount, info);
    }

    public void Dispose()
    {
        LeaveRaid();
        if (ReferenceEquals(Current, this))
        {
            Current = null;
        }
    }

    private void HandleLocalGameStop(string profileId, string exitStatus, string exitName)
    {
        // 注意：此处刻意不设 !inRaid 门槛。Stop 可能晚于采样清场（LeaveRaid 已把 inRaid 置 false），
        // 加门槛会丢掉 extraction 事件；改以 profileId 匹配做防御。
        // Stop 的 profileId 为本地玩家；已知本地 profileId 且不匹配时忽略（防御其他来源）。
        if (!string.IsNullOrEmpty(currentProfileId)
            && !string.IsNullOrEmpty(profileId)
            && !string.Equals(profileId, currentProfileId, StringComparison.Ordinal))
        {
            return;
        }

        try
        {
            buffer.AppendExtraction(
                DateTime.UtcNow.Ticks,
                currentRaidId,
                new ExtractionPayload(exitName ?? string.Empty, exitStatus ?? string.Empty));
        }
        catch (Exception exception)
        {
            log.LogWarning($"Bridge event collector: extraction record failed: {exception.Message}");
        }
    }

    private void Subscribe(Player player)
    {
        if (player == null)
        {
            return;
        }

        ActiveHealthController controller;
        try
        {
            controller = player.ActiveHealthController;
        }
        catch (Exception exception)
        {
            log.LogWarning($"Bridge event collector: health controller read failed: {exception.Message}");
            return;
        }

        if (controller == null)
        {
            return;
        }

        if (subscribedControllers.Contains(controller.Pointer))
        {
            return;
        }

        // 仅死亡事件需委托订阅（blittable EDamageType，可封送）；受伤改由 Harmony 补丁采集。
        // 独立 try/catch：此处失败不牵连其他玩家的订阅。
        try
        {
            var died = DelegateSupport.ConvertDelegate<Il2CppSystem.Action<EDamageType>>(
                new Action<EDamageType>(damageType => OnDeath(player, damageType)));

            controller.add_DiedEvent(died);
            subscribedControllers.Add(controller.Pointer);
            subscriptions.Add(new Subscription(controller, died));
        }
        catch (Exception exception)
        {
            log.LogWarning($"Bridge event collector: death subscribe failed: {exception.Message}");
        }
    }

    private void OnPersonAdd(IPlayer person)
    {
        try
        {
            var player = person != null ? person.TryCast<Player>() : null;
            if (player != null)
            {
                Subscribe(player);
            }
        }
        catch (Exception exception)
        {
            log.LogWarning($"Bridge event collector: OnPersonAdd handler failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 伤害 prefix（<see cref="ApplyDamagePatch.Prefix"/>）：结算前解析攻击者并记录归属，
    /// 供随后的死亡事件消费。非 raid / 无效 controller / 非正伤害一律忽略。
    /// </summary>
    private void HandleDamageIncoming(ActiveHealthController controller, float amount, DamageInfo info)
    {
        if (!inRaid || controller == null || amount <= 0f)
        {
            return;
        }

        try
        {
            var victim = controller.Player;
            if (victim == null)
            {
                return;
            }

            var victimProfileId = victim.ProfileId ?? string.Empty;
            if (string.IsNullOrEmpty(victimProfileId))
            {
                return;
            }

            var killer = ResolveKiller(victim, info);
            if (!killer.HasValue)
            {
                return;
            }

            attribution.RecordDamage(currentRaidId, victimProfileId, Environment.TickCount64, killer.Value);
        }
        catch (Exception exception)
        {
            log.LogWarning($"Bridge event collector: damage prefix handler failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 伤害 postfix（<see cref="ApplyDamagePatch.Postfix"/>）：结算后输出受伤事件。
    /// 非 raid / 无效 controller / 非正伤害一律忽略。
    /// </summary>
    private void HandleDamageApplied(ActiveHealthController controller, EBodyPart part, float amount, DamageInfo info)
    {
        if (!inRaid || controller == null || amount <= 0f)
        {
            return;
        }

        try
        {
            var victim = controller.Player;
            if (victim == null)
            {
                return;
            }

            buffer.AppendDamage(
                DateTime.UtcNow.Ticks,
                currentRaidId,
                new DamagePayload(
                    victim.ProfileId ?? string.Empty,
                    victim.IsYourPlayer,
                    part.ToString(),
                    amount,
                    info.DamageType.ToString()));
        }
        catch (Exception exception)
        {
            log.LogWarning($"Bridge event collector: damage handler failed: {exception.Message}");
        }
    }

    private void OnDeath(Player victim, EDamageType damageType)
    {
        // 与受伤处理一致的门槛：死亡回调只在 raid 内订阅存续期触发，此处防御退出瞬间的迟到回调。
        if (!inRaid)
        {
            return;
        }

        try
        {
            var nowMs = Environment.TickCount64;
            var victimProfileId = victim != null ? (victim.ProfileId ?? string.Empty) : string.Empty;
            var victimIsLocal = victim != null && victim.IsYourPlayer;

            KillerInfo killer;
            var hasKiller = attribution.TryTakeKiller(currentRaidId, victimProfileId, nowMs, out killer);

            buffer.AppendDeath(
                DateTime.UtcNow.Ticks,
                currentRaidId,
                new DeathPayload(victimProfileId, victimIsLocal, damageType.ToString(), hasKiller, killer));
        }
        catch (Exception exception)
        {
            log.LogWarning($"Bridge event collector: death handler failed: {exception.Message}");
        }
    }

    /// <summary>
    /// 伤害来源 → 归属。优先 <c>DamageInfo.Player</c>（observer bridge），
    /// 回退按 <c>SourceId</c> 在存活玩家列表内查找；自伤 / 无来源返回 null（不猜）。
    /// </summary>
    private KillerInfo? ResolveKiller(Player victim, DamageInfo info)
    {
        var sourceId = info.SourceId;
        if (string.IsNullOrEmpty(sourceId))
        {
            return null;
        }

        var victimProfileId = victim != null ? victim.ProfileId : null;
        if (!string.IsNullOrEmpty(victimProfileId)
            && string.Equals(sourceId, victimProfileId, StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var bridge = info.Player;
            if (bridge != null)
            {
                var attacker = bridge.iPlayer != null ? bridge.iPlayer.TryCast<Player>() : null;
                if (attacker != null)
                {
                    return BuildKiller(attacker);
                }

                var nickname = bridge.Nickname;
                if (!string.IsNullOrEmpty(nickname))
                {
                    return new KillerInfo(sourceId, nickname, string.Empty, string.Empty, false);
                }
            }
        }
        catch (Exception exception)
        {
            log.LogDebug($"Bridge event collector: killer bridge resolve failed: {exception.Message}");
        }

        var players = subscribedWorld != null ? subscribedWorld.AllAlivePlayersList : null;
        if (players != null)
        {
            foreach (var candidate in players)
            {
                if (candidate != null && string.Equals(candidate.ProfileId, sourceId, StringComparison.Ordinal))
                {
                    return BuildKiller(candidate);
                }
            }
        }

        return new KillerInfo(sourceId, string.Empty, string.Empty, string.Empty, false);
    }

    private static KillerInfo BuildKiller(Player player)
    {
        var profileId = player.ProfileId ?? string.Empty;
        var name = string.Empty;
        var side = string.Empty;
        var role = string.Empty;

        var profile = player.Profile;
        if (profile != null)
        {
            name = profile.Nickname ?? string.Empty;
            side = profile.Side.ToString();

            var info = profile.Info;
            var settings = info != null ? info.Settings : null;
            if (settings != null)
            {
                role = settings.Role.ToString();
            }
        }

        return new KillerInfo(profileId, name, side, role, player.IsYourPlayer);
    }

    private void DetachWorld()
    {
        if (subscribedWorld != null && personAddAction != null)
        {
            try
            {
                subscribedWorld.remove_OnPersonAdd(personAddAction);
            }
            catch (Exception exception)
            {
                log.LogDebug($"Bridge event collector: OnPersonAdd unsubscribe failed: {exception.Message}");
            }
        }

        personAddAction = null;
        personAddSubscribed = false;
        subscribedWorld = null;

        foreach (var subscription in subscriptions)
        {
            try
            {
                subscription.Controller.remove_DiedEvent(subscription.Died);
            }
            catch (Exception exception)
            {
                log.LogDebug($"Bridge event collector: death unsubscribe failed: {exception.Message}");
            }
        }

        subscriptions.Clear();
        subscribedControllers.Clear();
        attribution.Clear();
    }

    private readonly struct Subscription
    {
        internal Subscription(ActiveHealthController controller, Il2CppSystem.Action<EDamageType> died)
        {
            Controller = controller;
            Died = died;
        }

        internal ActiveHealthController Controller { get; }

        internal Il2CppSystem.Action<EDamageType> Died { get; }
    }
}

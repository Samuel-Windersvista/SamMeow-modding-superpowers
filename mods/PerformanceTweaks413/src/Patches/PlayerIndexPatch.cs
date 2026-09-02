using System.Reflection;
using EFT;
using EFT.NextObservedPlayer;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P9: GameWorld 玩家查找 int 索引化。
    /// 原版：TryGetAlivePlayer(int, out Player) 每次调用 O(n) 全扫 allAlivePlayersByID
    /// （GameWorld.cs:921-933）；TryGetObservedPlayer 同理（:907-919）。多个系统每帧频繁调用。
    /// 补丁：RegisterPlayer/UnregisterPlayer Postfix 维护两个静态 int 索引字典（Player.Id /
    /// ObservedPlayerView.Id），查询改 O(1)。观察者注册走 ClientNetworkGameWorld.RegisterPlayer
    /// override（ClientNetworkGameWorld.cs:25-33），该 override 最终调用 base(GameWorld).RegisterPlayer，
    /// 故 Postfix 挂 base 上即全覆盖。
    /// 安全阀：索引与权威集合数量不一致（注册/注销滞后一帧）时，从权威集合全量重建再查，保证不错判；
    /// 索引未命中返回 false，与原版全扫找不到的语义一致。
    /// 战局结束 GameWorld.Clear 清空权威集合（GameWorld.cs:1936-1940），静态索引跨战局残留——
    /// 防御：RegisterPlayer 首次调用时若权威集合已空而索引非空则整表重置。
    /// 签名已按 4.1.2 树核实：TryGetAlivePlayer(int, out Player) :921、TryGetObservedPlayer(int, out ObservedPlayerView) :907、
    /// RegisterPlayer/UnregisterPlayer(IPlayer) :2008/:2056、AllAlivePlayersList :525、allObservedPlayersByID :533
    /// （注意 4.1.2 中 allObservedPlayersByID 的 key 是 ProfileId(string)，查询侧只遍历 Values，无影响）。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EFT/GameWorld.cs:907-933, 2008-2080
    /// </summary>
    internal static class PlayerIndexPatch
    {
        private static readonly System.Collections.Generic.Dictionary<int, Player> PlayerIndex =
            new System.Collections.Generic.Dictionary<int, Player>();

        private static readonly System.Collections.Generic.Dictionary<int, ObservedPlayerView> ObservedIndex =
            new System.Collections.Generic.Dictionary<int, ObservedPlayerView>();

        internal static MethodBase TargetRegister()
        {
            return AccessTools.Method(typeof(GameWorld), "RegisterPlayer");
        }

        internal static MethodBase TargetUnregister()
        {
            return AccessTools.Method(typeof(GameWorld), "UnregisterPlayer");
        }

        internal static MethodBase TargetTryGetAlive()
        {
            // 显式带参数类型：GameWorld 存在 TryGetAlivePlayerByID(string, out IPlayer) 重载，
            // 防止 AccessTools 按名字歧义匹配
            return AccessTools.Method(typeof(GameWorld), "TryGetAlivePlayer",
                new[] { typeof(int), typeof(Player).MakeByRefType() });
        }

        internal static MethodBase TargetTryGetObserved()
        {
            return AccessTools.Method(typeof(GameWorld), "TryGetObservedPlayer",
                new[] { typeof(int), typeof(ObservedPlayerView).MakeByRefType() });
        }

        // ReSharper disable InconsistentNaming
        public static void RegisterPostfix(GameWorld __instance, IPlayer iPlayer)
        {
            if (!PerfTweaksConfig.PlayerIndexEnabled.Value)
            {
                return;
            }
            try
            {
                // 新战局防御：战局结束时 Clear() 清空权威集合，静态索引跨战局残留 ->
                // 权威集合已空而索引非空时整表重置
                if (__instance.AllAlivePlayersList.Count == 0 && PlayerIndex.Count > 0)
                {
                    PlayerIndex.Clear();
                }
                if (__instance.allObservedPlayersByID.Count == 0 && ObservedIndex.Count > 0)
                {
                    ObservedIndex.Clear();
                }
                Player player = iPlayer as Player;
                if (player != null)
                {
                    PlayerIndex[player.Id] = player;
                    return;
                }
                ObservedPlayerView observed = iPlayer as ObservedPlayerView;
                if (observed != null)
                {
                    ObservedIndex[observed.Id] = observed;
                }
            }
            catch
            {
                // fail-open
            }
        }

        public static void UnregisterPostfix(GameWorld __instance, IPlayer iPlayer)
        {
            if (!PerfTweaksConfig.PlayerIndexEnabled.Value)
            {
                return;
            }
            try
            {
                Player player = iPlayer as Player;
                if (player != null)
                {
                    PlayerIndex.Remove(player.Id);
                    return;
                }
                ObservedPlayerView observed = iPlayer as ObservedPlayerView;
                if (observed != null)
                {
                    ObservedIndex.Remove(observed.Id);
                }
            }
            catch
            {
                // fail-open
            }
        }

        public static bool TryGetAlivePrefix(GameWorld __instance, int id, ref Player alivePlayer, ref bool __result)
        {
            if (!PerfTweaksConfig.PlayerIndexEnabled.Value)
            {
                return true;
            }
            try
            {
                RebuildIfDirty(__instance);
                if (PlayerIndex.TryGetValue(id, out Player player))
                {
                    alivePlayer = player;
                    __result = true;
                    return false;
                }
                // 与原版语义一致：全扫找不到返回 false
                __result = false;
                return false;
            }
            catch
            {
                // fail-open
            }
            return true;
        }

        public static bool TryGetObservedPrefix(GameWorld __instance, int id, ref ObservedPlayerView observedPlayer, ref bool __result)
        {
            if (!PerfTweaksConfig.PlayerIndexEnabled.Value)
            {
                return true;
            }
            try
            {
                RebuildIfDirty(__instance);
                if (ObservedIndex.TryGetValue(id, out ObservedPlayerView view))
                {
                    observedPlayer = view;
                    __result = true;
                    return false;
                }
                __result = false;
                return false;
            }
            catch
            {
                // fail-open
            }
            return true;
        }
        // ReSharper restore InconsistentNaming

        /// <summary>
        /// 索引与权威集合数量不一致（注册/注销滞后一帧）时，从权威集合全量重建，保证不错判。
        /// Player 侧权威 = AllAlivePlayersList（与 allAlivePlayersByID 同步维护，GameWorld.cs:525,2017-2021）；
        /// Observed 侧权威 = allObservedPlayersByID（public 字段，GameWorld.cs:533，ClientNetworkGameWorld.cs:29）。
        /// </summary>
        private static void RebuildIfDirty(GameWorld gameWorld)
        {
            if (gameWorld.AllAlivePlayersList.Count != PlayerIndex.Count)
            {
                PlayerIndex.Clear();
                foreach (Player player in gameWorld.AllAlivePlayersList)
                {
                    PlayerIndex[player.Id] = player;
                }
            }
            if (gameWorld.allObservedPlayersByID.Count != ObservedIndex.Count)
            {
                ObservedIndex.Clear();
                foreach (ObservedPlayerView view in gameWorld.allObservedPlayersByID.Values)
                {
                    ObservedIndex[view.Id] = view;
                }
            }
        }
    }
}

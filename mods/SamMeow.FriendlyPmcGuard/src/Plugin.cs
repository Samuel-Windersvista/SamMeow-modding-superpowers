using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SamMeow.SPT.FriendlyPmcGuard.Patches;

namespace SamMeow.SPT.FriendlyPmcGuard
{
    /// <summary>
    /// SPT 3.11.4 客户端护栏：friendlyPMC 缺口补丁。
    ///
    /// P2 — BotFollowerPlayer.Dismiss(bool) Prefix：
    ///   原方法在 `_bot.IsDead || BotState != Active` 时提前 return（:601-604），
    ///   但此时 BossToFollow 仍悬垂、AIBossPlayer.Followers 仍含该 bot。
    ///   Prefix 在该路径补齐 vanilla 等价清理（BossToFollow=null + Followers.Remove）。
    ///
    /// P4 — BossPlayers.AddFollower(...) Prefix：
    ///   已死 / 非 Active 的 bot 直接拒绝招募（return false → 默认返回 null）。
    ///   两处调用方已核实对 null 返回安全。
    ///
    /// friendlyPMC 类型/方法全部反射解析；未加载则跳过全部补丁并打 Info。
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class FriendlyPmcGuardPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.sammeow.friendlypmcguard";
        public const string ModName = "SamMeow FriendlyPmcGuard";
        public const string ModVersion = "0.1.0";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(ModGuid);

            Type followerPlayerType = AccessTools.TypeByName("friendlyPMC.Components.BotFollowerPlayer");
            Type bossPlayersType = AccessTools.TypeByName("friendlyPMC.Modules.BossPlayers");

            if (followerPlayerType == null || bossPlayersType == null)
            {
                Log.LogInfo("[FriendlyPmcGuard] friendlyPMC not loaded — patches skipped (0/2)");
                return;
            }

            int applied = 0;

            // P2: BotFollowerPlayer.Dismiss(bool warnPlayer = false)
            DismissDeadFollowerPatch.BotField = AccessTools.Field(followerPlayerType, "_bot");
            DismissDeadFollowerPatch.PlayerField = AccessTools.Field(followerPlayerType, "_player");
            MethodBase dismiss = AccessTools.Method(followerPlayerType, "Dismiss", new[] { typeof(bool) });
            applied += TryPatch(dismiss, typeof(DismissDeadFollowerPatch), "Prefix", null,
                "P2 BotFollowerPlayer.Dismiss 死跟班清理");

            // P4: BossPlayers.AddFollower(BotOwner, pitAIBossPlayer, bool, WildSpawnType, string)
            MethodBase addFollower = AccessTools.Method(bossPlayersType, "AddFollower");
            applied += TryPatch(addFollower, typeof(AddFollowerDeadGuardPatch), "Prefix", null,
                "P4 BossPlayers.AddFollower 死尸招募拦截");

            Log.LogInfo($"[FriendlyPmcGuard] applied {applied}/2 patches");
        }

        private int TryPatch(MethodBase target, Type patchClass, string prefixName, string postfixName, string label)
        {
            if (target == null)
            {
                Log.LogError($"[FriendlyPmcGuard] {label}: 找不到目标方法，补丁未应用");
                return 0;
            }
            try
            {
                MethodInfo prefix = prefixName != null
                    ? patchClass.GetMethod(prefixName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    : null;
                MethodInfo postfix = postfixName != null
                    ? patchClass.GetMethod(postfixName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    : null;
                _harmony.Patch(target,
                    prefix: prefix != null ? new HarmonyMethod(prefix) : null,
                    postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                Log.LogInfo($"[FriendlyPmcGuard] {label}: 已应用 -> {target.DeclaringType?.FullName}.{target.Name}");
                return 1;
            }
            catch (Exception ex)
            {
                Log.LogError($"[FriendlyPmcGuard] {label}: 应用失败 — {ex.Message}");
                return 0;
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}

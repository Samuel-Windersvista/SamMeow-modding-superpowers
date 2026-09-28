using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SamMeow.SPT.FollowerGuard.Patches;

namespace SamMeow.SPT.FollowerGuard
{
    /// <summary>
    /// SPT 3.11.4 客户端护栏：boss 死亡后跟班 NRE 刷屏 + 沿旧路径滑步。
    ///
    /// 根因（见设计文档 follower-nre-fix-design.md）：
    ///   BotOwner.Dispose() 先把 boss 的 Mover / PatrollingData / BotFollower 置 null；
    ///   BotFollower.Dispose() 在 boss 非存活路径直接 return false，留下悬垂 BossToFollow、
    ///   IsInited 仍为 true 的 PatrolDataFollower；脑层继续调 ManualUpdate →
    ///   GClass548.Update（boss PatrollingData==null）或 GClass545.method_3
    ///   （boss Mover==null）持续 NRE，且旧路径速度被钉在 0.7 → 滑步。
    ///
    /// 补丁（A + B + C，全部 Prefix/Postfix，无 Transpiler）：
    ///   A BotBoss.get_MoveSpeed Prefix —— Mover==null 返回安全值 0.5f（外部调用兜底）
    ///   B PatrolDataFollower.ManualUpdate Prefix —— 任一前置不满足即 StopMove 并跳过原方法
    ///   C BotFollower.Dispose Postfix —— __result==false 时补齐清理并恢复原生 simple 巡逻
    ///   D 访问器 null 短路 —— C 置空 followerAIBase 后防残余调用 NRE
    ///   P3 PatrolDataFollower.InitPlayer Postfix —— 玩家 boss 路径清空 followerAIBase（保留 IsInited）
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class FollowerGuardPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.sammeow.followerguard";
        public const string ModName = "SamMeow FollowerGuard";
        public const string ModVersion = "0.1.1";

        internal static ManualLogSource Log;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            _harmony = new Harmony(ModGuid);
            int applied = 0;

            applied += TryPatch(AccessTools.Method(typeof(BotBoss), "get_MoveSpeed"),
                typeof(BotBossMoveSpeedPatch), "Prefix", null, "A BotBoss.get_MoveSpeed 护栏");

            applied += TryPatch(AccessTools.Method(typeof(PatrolDataFollower), "ManualUpdate"),
                typeof(PatrolDataFollowerManualUpdatePatch), "Prefix", null, "B PatrolDataFollower.ManualUpdate 护栏");

            applied += TryPatch(AccessTools.Method(typeof(BotFollower), "Dispose"),
                typeof(BotFollowerDisposePatch), null, "Postfix", "C BotFollower.Dispose 缺口修补");

            applied += TryPatch(AccessTools.Method(typeof(PatrolDataFollower), "set_HaveProblems"),
                typeof(FollowerAccessorGuardPatch), "PrefixHaveProblems", null, "D1 set_HaveProblems 短路");
            applied += TryPatch(AccessTools.Method(typeof(PatrolDataFollower), "SetProblemsSomebody"),
                typeof(FollowerAccessorGuardPatch), "PrefixSetProblemsSomebody", null, "D2 SetProblemsSomebody 短路");
            applied += TryPatch(AccessTools.Method(typeof(PatrolDataFollower), "SetFolloweType"),
                typeof(FollowerAccessorGuardPatch), "PrefixSetFolloweType", null, "D3 SetFolloweType 短路");
            applied += TryPatch(AccessTools.Method(typeof(PatrolDataFollower), "SetCloseToTarget"),
                typeof(FollowerAccessorGuardPatch), "PrefixSetCloseToTarget", null, "D4 SetCloseToTarget 短路");

            applied += TryPatch(AccessTools.Method(typeof(PatrolDataFollower), "InitPlayer"),
                typeof(PatrolDataFollowerInitPlayerPatch), null, "Postfix", "P3 PatrolDataFollower.InitPlayer 玩家路径 followerAIBase 清空");

            Log.LogInfo($"[FollowerGuard] applied {applied}/8 patches");
        }

        private int TryPatch(MethodBase target, Type patchClass, string prefixName, string postfixName, string label)
        {
            if (target == null)
            {
                Log.LogError($"[FollowerGuard] {label}: 找不到目标方法，补丁未应用");
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
                Log.LogInfo($"[FollowerGuard] {label}: 已应用 -> {target.DeclaringType?.Name}.{target.Name}");
                return 1;
            }
            catch (Exception ex)
            {
                Log.LogError($"[FollowerGuard] {label}: 应用失败 — {ex.Message}");
                return 0;
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}

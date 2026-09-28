using HarmonyLib;

namespace SamMeow.SPT.FollowerGuard.Patches
{
    /// <summary>
    /// 附加加固（配合方案 C）：访问器 null 短路。
    ///
    /// 方案 C 将 followerAIBase 置 null 后，以下原生访问器没有 null 检查，
    /// 若 boss 逻辑残余调用即 NRE：
    ///   - PatrolDataFollower.set_HaveProblems
    ///   - PatrolDataFollower.SetProblemsSomebody
    ///   - PatrolDataFollower.SetFolloweType
    ///   - PatrolDataFollower.SetCloseToTarget
    ///
    /// Prefix 在 followerAIBase==null 时返回 false 跳过原方法（幂等、无副作用）。
    /// </summary>
    internal static class FollowerAccessorGuardPatch
    {
        private static bool Guard(PatrolDataFollower instance)
        {
            return instance != null && instance.followerAIBase != null;
        }

        internal static bool PrefixHaveProblems(PatrolDataFollower __instance)
        {
            return Guard(__instance);
        }

        internal static bool PrefixSetProblemsSomebody(PatrolDataFollower __instance)
        {
            return Guard(__instance);
        }

        internal static bool PrefixSetFolloweType(PatrolDataFollower __instance)
        {
            return Guard(__instance);
        }

        internal static bool PrefixSetCloseToTarget(PatrolDataFollower __instance)
        {
            return Guard(__instance);
        }
    }
}

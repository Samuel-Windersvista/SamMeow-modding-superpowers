using HarmonyLib;

namespace SamMeow.SPT.FollowerGuard.Patches
{
    /// <summary>
    /// 方案 A：BotBoss.get_MoveSpeed 护栏。
    ///
    /// BotOwner.Dispose() 把 boss 的 Mover 置 null 之后，任何外部读取 boss.MoveSpeed
    /// （旧栈签名 BotBoss.get_MoveSpeed ← GClass545.method_3）都会 NRE。
    /// Prefix 在 Mover==null 时返回安全速度 0.5f 并跳过原方法。
    /// </summary>
    [HarmonyPatch(typeof(BotBoss), "get_MoveSpeed")]
    internal static class BotBossMoveSpeedPatch
    {
        private static bool Prefix(BotBoss __instance, ref float __result)
        {
            try
            {
                if (__instance == null || __instance.botOwner_0 == null || __instance.botOwner_0.Mover == null)
                {
                    __result = 0.5f;
                    return false;
                }
            }
            catch
            {
                // 访问链任一环节抛异常（含 botOwner_0 已回收）同样按 Mover 缺失处理。
                __result = 0.5f;
                return false;
            }
            return true;
        }
    }
}

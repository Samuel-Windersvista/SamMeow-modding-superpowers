using EFT;
using HarmonyLib;

namespace SamMeow.SPT.FollowerGuard.Patches
{
    /// <summary>
    /// 方案 B：PatrolDataFollower.ManualUpdate 统一更新保护（Prefix，跳过原方法）。
    ///
    /// 覆盖两条 NRE 栈：
    ///   - GClass548.Update：boss.PatrollingData == null（boss 已 Dispose）
    ///   - GClass545.method_3：boss.Mover == null（boss 已 Dispose）
    /// 同时覆盖 GClass242.UpdateNodeByBrain（无 IsInited 守卫，直调 ManualUpdate）。
    ///
    /// 任一前置条件不满足 → SafeStop(bot) 止血滑步 + return false 跳过原方法。
    /// 玩家 boss 路径（IsAI==false）保留原逻辑，仅补 followerPlayerBase null 检查。
    /// </summary>
    [HarmonyPatch(typeof(PatrolDataFollower), "ManualUpdate")]
    internal static class PatrolDataFollowerManualUpdatePatch
    {
        private static bool Prefix(PatrolDataFollower __instance)
        {
            try
            {
                if (__instance == null)
                {
                    return false;
                }

                BotOwner bot = __instance.botOwner_0;
                if (bot == null)
                {
                    return false;
                }

                BotFollower bf = bot.BotFollower;
                if (bf == null)
                {
                    return false;
                }

                IBossToFollow boss = bf.BossToFollow;
                if (boss == null)
                {
                    SafeStop(bot);
                    return false;
                }

                if (boss.IsAI)
                {
                    if (__instance.followerAIBase == null)
                    {
                        SafeStop(bot);
                        return false;
                    }
                    if (boss.PatrollingData == null)
                    {
                        SafeStop(bot);
                        return false;
                    }
                    return true;
                }

                // 玩家 boss 路径
                if (__instance.followerPlayerBase == null)
                {
                    SafeStop(bot);
                    return false;
                }
                return true;
            }
            catch
            {
                // 护栏自身异常：跳过原方法，避免把异常升级成每帧 NRE 刷屏。
                return false;
            }
        }

        private static void SafeStop(BotOwner bot)
        {
            try
            {
                bot.StopMove();
            }
            catch
            {
                // bot.Mover 可能已 null；StopMove 抛异常时静默。
            }
        }
    }
}

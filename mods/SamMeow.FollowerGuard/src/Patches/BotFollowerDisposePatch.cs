using EFT;
using HarmonyLib;

namespace SamMeow.SPT.FollowerGuard.Patches
{
    /// <summary>
    /// 方案 C：BotFollower.Dispose 缺口修补（Postfix，读 __result）。
    ///
    /// 原方法在 boss 非存活路径直接 return false：BossToFollow 悬垂、IsInited 仍为 true、
    /// followerAIBase 非 null，脑层因此持续走 follower 分支 → NRE + 滑步。
    ///
    /// __result == false 时补齐清理：
    ///   1. boss.RemoveFollower(bot) + BossToFollow = null
    ///   2. PatrolDataFollower.IsInited=false / followerAIBase=null / followerPlayerBase=null
    ///   3. follower 存活且 Active → 恢复原生 PatrolMode.simple 巡逻
    ///
    /// 依赖方案 B：IsInited=false 后 GClass242 仍会直调 ManualUpdate，B 的 guard 兜住 null 态。
    /// </summary>
    [HarmonyPatch(typeof(BotFollower), "Dispose")]
    internal static class BotFollowerDisposePatch
    {
        private static void Postfix(BotFollower __instance, bool __result)
        {
            if (__result || __instance == null)
            {
                return;
            }

            try
            {
                BotOwner bot = __instance.botOwner_0;
                IBossToFollow boss = __instance.BossToFollow;

                if (boss != null)
                {
                    // boss 可能已 Dispose；RemoveFollower 对已回收的 _followers 可能抛，静默。
                    try
                    {
                        boss.RemoveFollower(bot);
                    }
                    catch
                    {
                    }
                    __instance.BossToFollow = null;
                }

                PatrolDataFollower pdf = __instance._patrolDataFollower;
                if (pdf != null)
                {
                    pdf.IsInited = false;
                    pdf.followerAIBase = null;
                    pdf.followerPlayerBase = null;
                }

                if (bot != null
                    && bot.HealthController != null
                    && bot.HealthController.IsAlive
                    && bot.BotState == EBotState.Active)
                {
                    try
                    {
                        PatrolPointChooserBasic chooser =
                            PatrollingData.GetPointChooser(bot, PatrolMode.simple, bot.SpawnProfileData);
                        bot.PatrollingData?.SetMode(PatrolMode.simple, chooser);
                    }
                    catch
                    {
                        // SetMode 依赖 SpawnProfileData / BossLogic；失败时保持站桩（B 已止血）。
                    }
                }
            }
            catch
            {
                // 清理链自身异常不得逃逸到 BotOwner.Dispose 调用方。
            }
        }
    }
}

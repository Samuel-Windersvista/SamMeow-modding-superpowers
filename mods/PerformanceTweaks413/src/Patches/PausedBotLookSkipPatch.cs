using System.Reflection;
using EFT;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P3: 睡眠 bot 跳过感知。
    /// 原版漏洞：BotOwner.UpdateManual 中 LookSensor.ManualUpdate 在 paused 判断之前执行，
    /// 且感知任务经 AITaskManager 调度时不过滤 paused 状态——睡眠 bot 仍周期性做视线射线。
    /// 补丁：UpdateLook（AITaskManager 周期性调用的真实感知入口）在 bot 处于 paused 时直接跳过。
    /// 字段已按 4.1.2 树核实：LookSensor._botOwner public 字段 :18；BotOwner.StandBy :193、
    /// BotState :277、BotOwner.Id :328；EBotState.Active / BotStandByType.paused 存在。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/LookSensor.cs:307-310
    /// </summary>
    internal static class PausedBotLookSkipPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), "UpdateLook");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(LookSensor __instance)
        {
            if (!PerfTweaksConfig.PausedBotLookSkipEnabled.Value)
            {
                return true;
            }
            try
            {
                BotOwner owner = __instance._botOwner;
                if (owner != null
                    && owner.BotState == EBotState.Active
                    && owner.StandBy != null
                    && owner.StandBy.StandByType == BotStandByType.paused)
                {
                    return false;
                }
            }
            catch
            {
                // fail-open
            }
            return true;
        }
    }
}

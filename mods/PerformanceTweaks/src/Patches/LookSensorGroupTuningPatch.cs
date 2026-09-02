using System.Reflection;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P2: AITaskManager LookSensor 任务组参数放宽。
    /// 原版：构造时注册 LookSensor 组为 Class279(0.1s 默认周期, 10 平均任务, 0.6s 上限)。
    /// 补丁：ctor Postfix 整体替换该组配置（Class279 字段 readonly，只能换实例）。
    /// 证据：external/decompile-cache/eft-0.16-spt3114/AITaskManager.cs:180-184, Class279 定义 :41-77
    /// </summary>
    internal static class LookSensorGroupTuningPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Constructor(typeof(AITaskManager));
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(AITaskManager __instance)
        {
            if (!PerfTweaksConfig.LookGroupTuningEnabled.Value)
            {
                return;
            }
            try
            {
                __instance._regularTasks[EAITaskGroupType.LookSensor] = new AITaskManager.Class279(
                    PerfTweaksConfig.LookGroupPeriod.Value,
                    10,
                    PerfTweaksConfig.LookGroupMaxPeriod.Value);
            }
            catch
            {
                // fail-open
            }
        }
    }
}

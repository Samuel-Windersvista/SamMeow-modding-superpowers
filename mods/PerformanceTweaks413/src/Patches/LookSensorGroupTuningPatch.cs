using System.Reflection;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P2: AITaskManager LookSensor 任务组参数放宽（默认关闭）。
    /// 原版：构造时注册 LookSensor 组为 AIRegularTaskGroup(0.1s 默认周期, 10 平均任务, 0.6s 上限)。
    /// 补丁：ctor Postfix 整体替换该组配置（AIRegularTaskGroup 字段全 readonly，只能换实例）。
    /// 类型已按 4.1.2 树核实：AITaskManager.AIRegularTaskGroup 构造 :70，_regularTasks public 字典 :167，
    /// LookSensor 组注册 :183。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/AITaskManager.cs:167-186
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
                __instance._regularTasks[EAITaskGroupType.LookSensor] = new AITaskManager.AIRegularTaskGroup(
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

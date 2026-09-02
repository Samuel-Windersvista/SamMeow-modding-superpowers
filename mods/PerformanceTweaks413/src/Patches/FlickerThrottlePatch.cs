using System;
using System.Reflection;
using EFT.Visual;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P8: FlickerSystem.UpdateComponent 灯光闪烁分帧。
    /// 原版：ComponentSystem.Update 每帧遍历所有已注册组件调 UpdateComponent
    /// （ComponentSystem.cs:32-42），FlickerSystem.UpdateComponent 调 component.ManualUpdate()
    /// （FlickerSystem.cs:11-14）。大型战局灯光闪烁组件（路灯/应急灯等）可达数百，每帧全量更新。
    /// 补丁：Prefix 按 GetInstanceID 分帧——component.GetInstanceID() % N != Time.frameCount % N
    /// 时跳过本次（N 默认 2，可配 1-8；N=1 不降频）。闪烁动画本身是低频随机强度变化，
    /// 单组件降到 1/2~1/8 帧率视觉上不可察觉。GetInstanceID 取绝对值：Unity 运行时实例 ID 恒正，
    /// 防御性取模保证负值（编辑器环境）不导致永久跳过。
    /// 签名已按 4.1.2 树核实：FlickerSystem.UpdateComponent(Flicker)（FlickerSystem.cs:11-14）。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EFT/Visual/FlickerSystem.cs:11-14
    /// </summary>
    internal static class FlickerThrottlePatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(FlickerSystem), "UpdateComponent");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(Flicker component)
        {
            if (!PerfTweaksConfig.FlickerThrottleEnabled.Value)
            {
                return true;
            }
            try
            {
                int n = PerfTweaksConfig.FlickerThrottleDivisor.Value;
                if (n > 1 && component != null && Math.Abs(component.GetInstanceID()) % n != Time.frameCount % n)
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

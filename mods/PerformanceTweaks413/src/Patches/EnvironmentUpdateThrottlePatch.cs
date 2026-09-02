using System.Reflection;
using EFT.EnvironmentEffect;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P10: EnvironmentManager.Update 环境管理器降频。
    /// 原版：每帧执行 QualitySettings.shadowDistance 赋值 + Prism 曝光 SmoothDamp 推进
    /// （EnvironmentManager.cs:222-230）。阴影距离与曝光适应是视觉缓变，每帧刷新无必要。
    /// 注意：该方法包含每帧必须推进的状态（SmoothDamp 的 velocity ref 参数），在禁用 Transpiler
    /// 原则下不能单独抠掉某一行，故整体降频——每 N 帧只放行 1 帧（N 默认 2，可配 1-4；N=1 不降频）。
    /// 副作用：阴影距离过渡与曝光适应以 1/N 速率推进，视觉上几乎不可察觉；
    /// 若设 4 帧间隔且环境剧变（室内外切换），曝光过渡比原版慢约 4 倍，仍平滑无跳变。
    /// 签名已按 4.1.2 树核实：EnvironmentManager.Update()（EnvironmentManager.cs:222-230）。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EFT/EnvironmentEffect/EnvironmentManager.cs:222-230
    /// </summary>
    internal static class EnvironmentUpdateThrottlePatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EnvironmentManager), "Update");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(EnvironmentManager __instance)
        {
            if (!PerfTweaksConfig.EnvironmentThrottleEnabled.Value)
            {
                return true;
            }
            try
            {
                int n = PerfTweaksConfig.EnvironmentThrottleDivisor.Value;
                if (n > 1 && Time.frameCount % n != 0)
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

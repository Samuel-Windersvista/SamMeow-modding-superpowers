using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P6: CameraClass.SetCamera 相机重复设置去重。
    /// 原版:Streamer.Update 每帧调用 CameraClass.Instance.SetCamera(Camera.main)
    /// (Streamer.cs:133),SetCamera 内部无条件 Release/重建 VRamUsageWrapper + Reset +
    /// 重建相机组件栈 + 触发 OnCameraChanged 委托(CameraClass.cs:559-582)。
    /// 相机未切换时这些工作每帧重复执行,纯浪费;且 OnCameraChanged 委托反复触发有累积风险。
    /// 补丁:Prefix 在 camera 非空且与当前 __instance.Camera 相同时直接跳过(return false);
    /// 首次设置(Camera 仍为 null)或相机实际切换时放行原逻辑。
    /// 证据:external/decompile-cache/eft-0.16-spt3114/CameraClass.cs:559-582, Camera 属性 :323,
    ///       Streamer.cs:133
    /// </summary>
    internal static class CameraSetDedupPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(CameraClass), "SetCamera");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(CameraClass __instance, Camera camera)
        {
            if (!PerfTweaksConfig.CameraSetDedupEnabled.Value)
            {
                return true;
            }
            try
            {
                if (camera != null && camera == __instance.Camera)
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

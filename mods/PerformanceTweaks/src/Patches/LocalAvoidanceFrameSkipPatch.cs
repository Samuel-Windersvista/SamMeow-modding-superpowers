using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P14: AI 扎堆斥力分帧（拥挤时）。
    /// 原版：每个 bot 每帧遍历同体素所有 bot 做互斥计算（GClass476.ManualUpdate，
    /// GClass476.cs:47-87；BotMover.cs:273 每帧调用），扎堆场景（撤离点/门口/僵尸群）O(k²)。
    /// 补丁：体素内 bot 数达到拥挤阈值时，斥力循环隔帧执行——跳过的帧手动调用 method_0()
    /// 保持偏移衰减连续（method_0 是 public，GClass476.cs:89）。稀疏场景（&lt;4 个邻居）原样执行。
    /// 注意：不降频到 N&gt;2，斥力是防止 bot 站位重叠的唯一机制（议会风险标注）。
    /// </summary>
    internal static class LocalAvoidanceFrameSkipPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(GClass476), "ManualUpdate");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(GClass476 __instance)
        {
            if (!PerfTweaksConfig.AvoidanceFrameSkipEnabled.Value)
            {
                return true;
            }
            try
            {
                // 稀疏体素原样执行，成本本来就低
                if (__instance.botOwner_0?.VoxelesPersonalData?.CurVoxel == null
                    || __instance.botOwner_0.VoxelesPersonalData.CurVoxel.BotsInside.Count < PerfTweaksConfig.AvoidanceCrowdThreshold.Value)
                {
                    return true;
                }
                if (Time.frameCount % 2 != 0)
                {
                    // 跳帧：保持偏移衰减连续，只跳过斥力遍历循环
                    __instance.method_0();
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

using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P16: BotLocalAvoidance.ManualUpdate AI 扎堆斥力分帧（移植 3.11 P14）。
    /// 原版：每个 bot 每帧遍历同体素所有 bot 做互斥计算（BotLocalAvoidance.cs:47-91，
    /// 由 BotMover 每帧调用），扎堆场景（撤离点/门口/僵尸群）O(k²)。
    /// 补丁：体素内 bot 数达到拥挤阈值（默认 4）时斥力循环隔帧执行——跳过的帧手动调用
    /// OffsetDecrease()（4.1.2 反混淆后的衰减方法名，3.11 版为 method_0()）保持偏移衰减连续。
    /// 稀疏场景（&lt; 阈值个邻居）原样执行。4.1.2 版方法体自带的 NearDoor 短路（:49-52）
    /// 与存活检查（:54-57）在原方法体内保留，prefix 不触碰。
    /// 注意：不降频到 N&gt;2，斥力是防止 bot 站位重叠的唯一机制（议会风险标注）。
    /// 签名已按 4.1.2 树核实：BotLocalAvoidance.ManualUpdate()（全局命名空间类，BotLocalAvoidance.cs:47）；
    /// _owner public 字段（BotData.cs:7）、VoxelesPersonalData.CurVoxel（BotVoxelesPersonalData.cs:24）、
    /// CurVoxel.BotsInside 为 List&lt;BotOwner&gt;（NavGraphVoxelSimple.cs:26）。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/BotLocalAvoidance.cs:47-105
    /// </summary>
    internal static class LocalAvoidanceFrameSkipPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BotLocalAvoidance), "ManualUpdate");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(BotLocalAvoidance __instance)
        {
            if (!PerfTweaksConfig.AvoidanceFrameSkipEnabled.Value)
            {
                return true;
            }
            try
            {
                // 稀疏体素原样执行，成本本来就低
                if (__instance._owner?.VoxelesPersonalData?.CurVoxel == null
                    || __instance._owner.VoxelesPersonalData.CurVoxel.BotsInside.Count
                        < PerfTweaksConfig.AvoidanceCrowdThreshold.Value)
                {
                    return true;
                }
                if (Time.frameCount % 2 != 0)
                {
                    // 跳帧：保持偏移衰减连续，只跳过斥力遍历循环
                    __instance.OffsetDecrease();
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

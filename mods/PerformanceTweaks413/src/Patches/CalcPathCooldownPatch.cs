using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P4: BotMover.CalcPath 寻路冷却。
    /// 原版：bot 每次移动目标变化即 NavMesh.CalculatePath（同步、主线程、CPU 密集 A*）。
    /// 补丁：冷却窗口内且目标点未显著移动时返回上次缓存的路径角点；否则放行并在 Postfix 更新缓存。
    /// 缓存挂 ConditionalWeakTable，bot 销毁后自动回收，不泄漏。
    /// 签名已按 4.1.2 树核实：CalcPath(Vector3) : Vector3[]（BotMover.cs:457-474）。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/BotMover.cs:457-474
    /// </summary>
    internal static class CalcPathCooldownPatch
    {
        private sealed class PathCache
        {
            internal float Time;
            internal Vector3 Target;
            internal Vector3[] Corners;
        }

        private static readonly ConditionalWeakTable<BotMover, PathCache> Cache = new ConditionalWeakTable<BotMover, PathCache>();

        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BotMover), "CalcPath");
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(BotMover __instance, Vector3 position, ref Vector3[] __result)
        {
            if (!PerfTweaksConfig.PathCooldownEnabled.Value)
            {
                return true;
            }
            try
            {
                if (Cache.TryGetValue(__instance, out PathCache cache))
                {
                    float threshold = PerfTweaksConfig.PathRetargetThresholdMeters.Value;
                    bool fresh = Time.time - cache.Time < PerfTweaksConfig.PathCooldownSeconds.Value;
                    bool sameTarget = (position - cache.Target).sqrMagnitude < threshold * threshold;
                    if (fresh && sameTarget)
                    {
                        __result = cache.Corners;
                        return false;
                    }
                }
            }
            catch
            {
                // fail-open
            }
            return true;
        }

        public static void Postfix(BotMover __instance, Vector3 position, Vector3[] __result)
        {
            if (!PerfTweaksConfig.PathCooldownEnabled.Value)
            {
                return;
            }
            try
            {
                PathCache cache = Cache.GetOrCreateValue(__instance);
                cache.Time = Time.time;
                cache.Target = position;
                cache.Corners = __result;
            }
            catch
            {
                // fail-open
            }
        }
        // ReSharper restore InconsistentNaming
    }
}

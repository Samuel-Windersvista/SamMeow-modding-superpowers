using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P12: GClass895.Class558.method_2 弹道可见性查询按帧缓存。
    /// 原版:每个观察玩家每帧调用一次 method_2(ISharedBallisticsCalculator),
    /// 内部 O(ActiveShotsCount) 遍历所有活跃子弹做距离+角度判断
    /// (GClass895.cs:80-92)。同一帧内同一实例的多次调用结果必然相同。
    /// 补丁:Prefix+Postfix 按帧缓存——ConditionalWeakTable 挂 {帧号, 结果},
    /// 同帧内第二次调用直接返回缓存;Postfix 更新缓存为当前帧与 __result。
    /// ConditionalWeakTable 键为实例弱引用,Class558 销毁后自动回收,不泄漏。
    /// 风险:子弹在帧内移动,缓存有效期 = N 帧(默认 2,可配 1-5;N=1 不缓存),
    /// 弹道可见性开关延迟最多 N 帧,视觉上不可察觉。
    /// 证据:external/decompile-cache/eft-0.16-spt3114/GClass895.cs:80-92, :17-49
    /// </summary>
    internal static class BulletCullingSnapshotPatch
    {
        private sealed class FrameCache
        {
            internal int Frame = int.MinValue; // 保证首次调用不命中缓存
            internal bool Result;
        }

        private static readonly ConditionalWeakTable<GClass895.Class558, FrameCache> Cache =
            new ConditionalWeakTable<GClass895.Class558, FrameCache>();

        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(GClass895.Class558), "method_2");
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(GClass895.Class558 __instance, ref bool __result)
        {
            if (!PerfTweaksConfig.BulletCullingSnapshotEnabled.Value)
            {
                return true;
            }
            try
            {
                int n = PerfTweaksConfig.BulletCullingSnapshotInterval.Value;
                if (n <= 1)
                {
                    return true; // N=1 不缓存
                }
                if (Cache.TryGetValue(__instance, out FrameCache cache)
                    && Time.frameCount - cache.Frame < n)
                {
                    __result = cache.Result;
                    return false;
                }
            }
            catch
            {
                // fail-open
            }
            return true;
        }

        public static void Postfix(GClass895.Class558 __instance, bool __result)
        {
            if (!PerfTweaksConfig.BulletCullingSnapshotEnabled.Value)
            {
                return;
            }
            try
            {
                int n = PerfTweaksConfig.BulletCullingSnapshotInterval.Value;
                if (n <= 1)
                {
                    return;
                }
                FrameCache cache = Cache.GetOrCreateValue(__instance);
                cache.Frame = Time.frameCount;
                cache.Result = __result;
            }
            catch
            {
                // fail-open
            }
        }
        // ReSharper restore InconsistentNaming
    }
}

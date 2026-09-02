using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P12: CullingStateToggle.CheckBallistic 弹道可见性查询按帧缓存。
    /// 原版：每个观察玩家每帧调用一次 CheckBallistic(IBallisticsCalculator)，
    /// 内部 O(ActiveShotsCount) 遍历所有活跃子弹做距离+角度判断
    /// （BasePlayerCulling.cs:81-93）。同一帧内同一实例的多次调用结果必然相同。
    /// 补丁：Prefix+Postfix 按帧缓存——ConditionalWeakTable 挂 {帧号, 结果}，
    /// 同帧内第二次调用直接返回缓存；Postfix 更新缓存为当前帧与 __result。
    /// ConditionalWeakTable 键为实例弱引用，CullingStateToggle 销毁后自动回收，不泄漏。
    /// 风险：子弹在帧内移动，缓存有效期 = N 帧（默认 2，可配 1-5；N=1 不缓存），
    /// 弹道可见性开关延迟最多 N 帧，视觉上不可察觉。
    /// 签名已按 4.1.2 树核实（与改名映射表有差异，需修正）：
    /// 目标类型 = BasePlayerCulling 的嵌套类 CullingStateToggle（用 AccessTools.Inner 获取），
    /// 方法 = CheckBallistic(IBallisticsCalculator)（BasePlayerCulling.cs:81）。
    /// 注意：4.1.2 树中不存在 ISharedBallisticsCalculator 接口——CheckBallisticState 传的是
    /// Singleton&lt;GameWorld&gt;.Instance.SharedBallisticsCalculator（IBallisticsCalculator 类型，
    /// EFT.Ballistics 命名空间），参数类型按实际代码修正为 IBallisticsCalculator。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/BasePlayerCulling.cs:81-93
    /// </summary>
    internal static class BulletCullingSnapshotPatch
    {
        private sealed class FrameCache
        {
            internal int Frame = int.MinValue; // 保证首次调用不命中缓存
            internal bool Result;
        }

        private static readonly ConditionalWeakTable<BasePlayerCulling.CullingStateToggle, FrameCache> Cache =
            new ConditionalWeakTable<BasePlayerCulling.CullingStateToggle, FrameCache>();

        internal static MethodBase TargetMethod()
        {
            System.Type inner = AccessTools.Inner(typeof(BasePlayerCulling), "CullingStateToggle");
            return inner != null ? AccessTools.Method(inner, "CheckBallistic") : null;
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(BasePlayerCulling.CullingStateToggle __instance, ref bool __result)
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

        public static void Postfix(BasePlayerCulling.CullingStateToggle __instance, bool __result)
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

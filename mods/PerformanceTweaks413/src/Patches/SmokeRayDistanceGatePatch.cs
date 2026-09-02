using System.Reflection;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P13: BotsSmokesVisionSystem.IsRayIntersectAnySmoke 距离门限。
    /// 原版：4.1 新增烟雾系统，每条 AI 视线检查都追加一次 IsRayIntersectAnySmoke
    /// （EnemyPartVision.cs:182 在 DoCastAndCacheObstacles 内调用），内部做体素遍历
    /// （GetSmokeGrenadesAlongRayNonAlloc，BotsSmokesVisionSystem.cs:244-294）+ 逐烟求交。
    /// 补丁：Prefix 先算射线长度（from→to 即 bot→目标距离），超过阈值（默认 150m）
    /// 直接返回"无遮挡"（__result=false + out 默认值），跳过体素遍历。
    /// 无烟雾时原方法有 _affectedVoxelsMap.Count==0 短路（BotsSmokesVisionSystem.cs:247-250），
    /// 本门限只挡远距离烟雾检查，不碰无烟短路。
    /// 语义说明：跳过时 grenadesAlongWayCount 置 0（与"无烟"结果一致）；该字段仅用于调试输出
    /// （EnemyPartVision.cs:418/454 DebugStruct），超远距离的烟雾遮挡本就不可感知。
    /// 签名已按 4.1.2 树核实：IsRayIntersectAnySmoke(Vector3 from, Vector3 to, out SphereCollider,
    /// out Vector3, out int)（EFT/BotsSmokesVisionSystem.cs:122），from/to 直接给出距离信息。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EFT/BotsSmokesVisionSystem.cs:122-146
    /// </summary>
    internal static class SmokeRayDistanceGatePatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BotsSmokesVisionSystem), "IsRayIntersectAnySmoke");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(Vector3 from, Vector3 to,
            ref SphereCollider hitCollider, ref Vector3 intersectionPoint, ref int grenadesAlongWayCount,
            ref bool __result)
        {
            if (!PerfTweaksConfig.SmokeRayDistanceGateEnabled.Value)
            {
                return true;
            }
            try
            {
                float limitM = PerfTweaksConfig.SmokeRayDistanceMeters.Value;
                float limitSq = limitM * limitM;
                if ((to - from).sqrMagnitude > limitSq)
                {
                    hitCollider = null;
                    intersectionPoint = Vector3.zero;
                    grenadesAlongWayCount = 0;
                    __result = false;
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

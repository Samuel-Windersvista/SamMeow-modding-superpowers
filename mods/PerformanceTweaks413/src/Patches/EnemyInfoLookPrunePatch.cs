using System.Reflection;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P1: EnemyInfo.CheckLookEnemy 感知距离修剪。
    /// 原版：bot 对记忆里每个敌人做多部位 Linecast + 可见时 2 条 Raycast，敌人名单只随阵亡移除。
    /// 补丁：敌人超出 bot 当前视距 * 倍率 且当前不可见时，跳过本次射线检查（视距外不可能被看见，
    /// 检查必然全部落空）。当前可见的敌人不跳过，避免状态残留。
    /// 签名已按 4.1.2 树核实：CheckLookEnemy(LookAllData, float)（EnemyInfo.cs:493）；
    /// IsVisible/Owner/Person/CurrPosition 属性 :99/196/105/166，Owner.LookSensor.VisibleDist :114。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EnemyInfo.cs:493-580
    /// </summary>
    internal static class EnemyInfoLookPrunePatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(EnemyInfo), "CheckLookEnemy");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(EnemyInfo __instance)
        {
            if (!PerfTweaksConfig.LookPruneEnabled.Value)
            {
                return true;
            }
            try
            {
                if (__instance.IsVisible)
                {
                    return true;
                }
                BotOwner owner = __instance.Owner;
                if (owner?.LookSensor == null || __instance.Person?.Transform == null)
                {
                    return true;
                }
                float maxDist = owner.LookSensor.VisibleDist * PerfTweaksConfig.LookPruneDistanceMultiplier.Value;
                Vector3 delta = __instance.CurrPosition - owner.Position;
                if (delta.sqrMagnitude > maxDist * maxDist)
                {
                    return false;
                }
            }
            catch
            {
                // fail-open：补丁异常时放行原逻辑
            }
            return true;
        }
    }
}

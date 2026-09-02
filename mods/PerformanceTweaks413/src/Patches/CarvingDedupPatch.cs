using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P15: NavMeshObstacle.carving 同帧重复赋值去重。
    /// 病灶评估结论：**"同一 NavMeshObstacle 同帧并发重复赋值"实际不成立**，本补丁按规格
    /// 保留为"已实现但默认关闭"的防御性补丁。复查证据（4.1.2 树）：
    ///   1. 门 carving：NavMeshDoorLink.TryCarve 有 1s 节流 + 2m 无 bot 判定（NavMeshDoorLink.cs:305-324）；
    ///      赋值只在事件（OnDoorStateChanged/SetDoor/CarveOff/SetAllCarvers）或带 carving 守卫的
    ///      ManualUpdate（:128-134）里发生；BotDoorsController.Update（:102-111）每帧只调 ManualUpdate。
    ///   2. 路障切割：NavMeshCutElement.Update 由 _unCutTime 节流（默认 CutPeriodSec=300s，
    ///      NavMeshCutElement.cs:39-45），Cut/UnCut（NavMeshCutGroup.cs:62-73）事件驱动；
    ///      bot 死亡触发路径经 OnBotRemove（NavMeshCutController.cs:37-51）。
    ///   3. 火车：TrainNavMeshCutter.OnTrainCome 事件驱动（火车状态切换低频）。
    ///   4. Unity 原生 carving setter 值相等时本身就是 no-op，同值冗余无需补丁。
    /// 计划文档 N3 的真病灶是"不同 obstacle 同帧并发 carving"——消除它需要延迟合批，被议会红线
    /// 禁止（门已开但导航没更新 = bot 卡门）。故本补丁只做同帧同值去重，不改任何时序语义。
    /// 实现：patch carving 的托管 setter（统一入口，覆盖门/切割/火车全部赋值点），
    /// ConditionalWeakTable 记录 (obstacle, frame, value)：
    ///   - 目标值 == 当前值：跳过（Unity 原生本就是 no-op）；
    ///   - 同帧已处理且目标值相同：跳过（同帧冗余）；
    ///   - 同帧已处理但目标值不同：放行覆盖（值变化必须生效，否则门开了导航却不更新）；
    ///   - 新帧：放行并记录。
    /// 注意：Unity 原生 set_carving 是 InternalCall 方法，若 Harmony 无法 patch 则 TryPatch 记失败，
    /// 补丁未应用，fail-open 不影响其余补丁。
    /// </summary>
    internal static class CarvingDedupPatch
    {
        private sealed class CarveState
        {
            internal int Frame = int.MinValue; // 保证首次调用不命中"同帧"分支
            internal bool LastValue;
        }

        private static readonly ConditionalWeakTable<NavMeshObstacle, CarveState> States =
            new ConditionalWeakTable<NavMeshObstacle, CarveState>();

        internal static MethodBase TargetMethod()
        {
            return AccessTools.PropertySetter(typeof(NavMeshObstacle), "carving");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(NavMeshObstacle __instance, bool value)
        {
            if (!PerfTweaksConfig.CarvingDedupEnabled.Value)
            {
                return true;
            }
            try
            {
                if (__instance == null)
                {
                    return true;
                }
                if (__instance.carving == value)
                {
                    // 目标值 == 当前值：Unity 原生 setter 本就是 no-op，直接跳过
                    return false;
                }
                CarveState state = States.GetOrCreateValue(__instance);
                if (state.Frame == Time.frameCount)
                {
                    if (state.LastValue == value)
                    {
                        // 同帧已赋过同样的值：冗余，跳过
                        return false;
                    }
                    // 同帧值变化：必须放行（覆盖），否则门开了导航却不更新
                    state.LastValue = value;
                    return true;
                }
                state.Frame = Time.frameCount;
                state.LastValue = value;
            }
            catch
            {
                // fail-open
            }
            return true;
        }
    }
}

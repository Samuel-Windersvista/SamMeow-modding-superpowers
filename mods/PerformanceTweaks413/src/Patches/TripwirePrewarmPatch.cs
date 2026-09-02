using System.Reflection;
using EFT;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P11: GameWorld.OnGameStarted 绊线规划器预热。
    /// 原版：Player.CreatePlantPlanner() 首次调用时同步 Resources.Load("Prefabs/tripwire_planner")
    /// 并 Instantiate（Player.cs:26489-26501），首次放绊线时造成明显卡顿。
    /// 补丁：OnGameStarted Postfix 在战局开始时即调用主玩家 CreatePlantPlanner()，
    /// 把同步加载前移到加载阶段（那里本来就有大量加载）；方法内部有
    /// TripwirePlanner == null 懒加载守卫（:26491），重复调用安全，仅更新位置。
    /// 包 try/catch：预热失败不影响战局。代价：不主动放绊线时也会多一个闲置
    /// TripwireVisualPlacer 对象（空 GameObject + 组件），成本可忽略。
    /// 签名已按 4.1.2 树核实：GameWorld.OnGameStarted() :2329，MainPlayer public 字段 :541，
    /// Player.CreatePlantPlanner() :26489（3.11 版 :570 MainPlayer 属性已改为字段，访问语法不变）。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EFT/Player.cs:26489-26501
    /// </summary>
    internal static class TripwirePrewarmPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(GameWorld), "OnGameStarted");
        }

        // ReSharper disable once InconsistentNaming
        public static void Postfix(GameWorld __instance)
        {
            if (!PerfTweaksConfig.TripwirePrewarmEnabled.Value)
            {
                return;
            }
            try
            {
                __instance.MainPlayer?.CreatePlantPlanner();
            }
            catch
            {
                // 预热失败不影响战局（fail-open）
            }
        }
    }
}

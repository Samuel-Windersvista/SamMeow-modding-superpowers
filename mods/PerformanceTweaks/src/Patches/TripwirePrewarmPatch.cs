using System.Reflection;
using EFT;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P11: GameWorld.OnGameStarted 绊线规划器预热。
    /// 原版:Player.CreatePlantPlanner() 首次调用时同步 Resources.Load("Prefabs/tripwire_planner")
    /// 并 Instantiate(Player.cs:26210-26222),首次放绊线时造成明显卡顿。
    /// 补丁:OnGameStarted Postfix 在战局开始时即调用主玩家 CreatePlantPlanner(),
    /// 把同步加载前移到加载阶段(那里本来就有大量加载);方法内部有
    /// TripwireVisualPlacer_0 == null 懒加载守卫(:26212),重复调用安全,仅更新位置。
    /// 包 try/catch:预热失败不影响战局。代价:不主动放绊线时也会多一个闲置
    /// TripwireVisualPlacer 对象(空 GameObject + 组件),成本可忽略。
    /// 证据:external/decompile-cache/eft-0.16-spt3114/EFT/Player.cs:26210-26222,
    ///       EFT/GameWorld.cs:2565-2568, MainPlayer :570
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
                // 预热失败不影响战局(fail-open)
            }
        }
    }
}

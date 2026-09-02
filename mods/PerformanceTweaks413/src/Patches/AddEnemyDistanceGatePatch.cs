using System.Reflection;
using EFT;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P5: AddEnemy 初始感知距离门限（默认关闭）。
    /// 原版：bot 激活时把战区内所有玩家登记进 EnemyInfos（onActivation=true），距离无上限，
    /// 导致全图 bot 对玩家持有记忆并周期性做感知射线。
    /// 补丁：仅门限 onActivation 的初始登记；枪声/受击/组内通报等事件驱动登记（onActivation=false）
    /// 不受影响——远处玩家一旦开火或被其它 bot 目击仍会正常进入感知。
    /// 类与签名已按 4.1.2 树核实：BotMemory.AddEnemy(IPlayer, BotGroupEnemyInfo, bool)（BotMemory.cs:609）；
    /// 持有 bot 用 public 字段 _owner :66（3.11 的 botOwner_0 已更名）。
    /// 与 SAIN 4.x 的 AddEnemy 守卫 prefix 条件正交可共存；为避免影响 SAIN 敌人登记链路，本补丁默认关闭。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EFT/BotMemory.cs:609-639
    /// </summary>
    internal static class AddEnemyDistanceGatePatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BotMemory), "AddEnemy");
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(BotMemory __instance, IPlayer enemy, bool onActivation)
        {
            if (!PerfTweaksConfig.AggroDistanceGateEnabled.Value)
            {
                return true;
            }
            if (!onActivation)
            {
                return true;
            }
            try
            {
                BotOwner owner = __instance._owner;
                if (owner == null || enemy?.Transform == null)
                {
                    return true;
                }
                float maxDist = PerfTweaksConfig.AggroDistanceMeters.Value;
                if ((owner.Position - enemy.Position).sqrMagnitude > maxDist * maxDist)
                {
                    return false;
                }
            }
            catch
            {
                // fail-open
            }
            return true;
        }
        // ReSharper restore InconsistentNaming
    }
}

using System.Reflection;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks413.Patches
{
    /// <summary>
    /// P14: Player.FBBIKUpdate 本地 bot IK 距离分档隔帧（移植 3.11 P13）。
    /// 原版：本地 bot 的 FBBIKUpdate 每帧无条件执行 _fbbik.solver.Update()（Player.cs:26329-26344，
    /// 由 :25839 每帧调用），而联机观察玩家路径有隔 3 帧（ObservedPlayerView.cs:442）——
    /// BSG 官方认可的降级。
    /// 补丁：按距离分档——40m 内每帧（手感区）；40-80m 隔 2 帧；80m 外隔 3 帧（对齐观察玩家语义）。
    /// 主玩家 FirstPerson 时 distance 恒为 0（Player.cs:25822-25824），近距离永远不受影响。
    /// 签名已按 4.1.2 树核实：Player.FBBIKUpdate(float distance)（EFT/Player.cs:26329），与 3.11 同形。
    /// 证据：external/decompile-cache/eft-0.16.9.5-spt412/EFT/Player.cs:26329-26344
    /// </summary>
    internal static class LocalBotFbbikFrameSkipPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), "FBBIKUpdate");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(Player __instance, float distance)
        {
            if (!PerfTweaksConfig.FbbikFrameSkipEnabled.Value)
            {
                return true;
            }
            try
            {
                // 主玩家/近距离全帧率，不做任何跳过
                if (distance < 40f)
                {
                    return true;
                }
                int interval = distance > 80f ? 3 : 2;
                if (Time.frameCount % interval != 0)
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
    }
}

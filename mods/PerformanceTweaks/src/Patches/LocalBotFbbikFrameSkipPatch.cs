using System.Reflection;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P13: 本地 bot FBBIK 距离分档隔帧。
    /// 原版：本地 bot 的 _fbbik.solver.Update() 每帧无条件执行（Player.cs:26050-26065），
    /// 而联机观察玩家路径有 frameCount % 3 隔帧（ObservedPlayerView.cs:847）——BSG 官方认可的降级。
    /// 补丁：按距离分档——40m 内每帧（手感区）；40-80m 隔 2 帧；80m 外隔 3 帧（对齐观察玩家语义）。
    /// 主玩家永远近距离不受影响。议会裁决进版项（2026-08-19）。
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

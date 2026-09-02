using System.Reflection;
using EFT;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P3: 鐫＄湢 bot 璺宠繃鎰熺煡銆?    /// 鍘熺増婕忔礊锛欱otOwner.UpdateManual 涓?LookSensor.ManualUpdate 鍦?paused 鍒ゆ柇涔嬪墠鎵ц锛?    /// 涓旀劅鐭ヤ换鍔＄粡 AITaskManager 璋冨害鏃朵笉杩囨护 paused 鐘舵€?鈥斺€?鐫＄湢 bot 浠嶅懆鏈熸€у仛瑙嗙嚎灏勭嚎銆?    /// 琛ヤ竵锛歎pdateLook锛圓ITaskManager 鍛ㄦ湡鎬ц皟鐢ㄧ殑鐪熷疄鎰熺煡鍏ュ彛锛夊湪 bot 澶勪簬 paused 鏃剁洿鎺ヨ烦杩囥€?    /// 璇佹嵁锛歟xternal/decompile-cache/eft-0.16-spt3114/EFT/BotOwner.cs:1014-1073,
    ///       LookSensor.cs:306-348 (AIPeriodicUpdate -> UpdateLook)
    /// </summary>
    internal static class PausedBotLookSkipPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(LookSensor), "UpdateLook");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(LookSensor __instance)
        {
            if (!PerfTweaksConfig.PausedBotLookSkipEnabled.Value)
            {
                return true;
            }
            try
            {
                BotOwner owner = __instance._botOwner;
                if (owner != null
                    && owner.BotState == EBotState.Active
                    && owner.StandBy != null
                    && owner.StandBy.StandByType == BotStandByType.paused)
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

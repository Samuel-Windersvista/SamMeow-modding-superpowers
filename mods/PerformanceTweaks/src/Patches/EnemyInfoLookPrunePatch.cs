using System.Reflection;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P1: EnemyInfo.CheckLookEnemy 璺濈淇壀銆?    /// 鍘熺増锛歜ot 瀵硅蹇嗕腑姣忎釜鏁屼汉鍋氬閮ㄤ綅 Linecast + 鍙鏃?2 鏉?Raycast锛屾晫浜哄悕鍗曞彧闅忔浜＄Щ闄ゃ€?    /// 琛ヤ竵锛氭晫浜鸿秴鍑?bot 褰撳墠瑙嗚窛 * 鍊嶇巼 涓斿綋鍓嶄笉鍙鏃讹紝璺宠繃鏈灏勭嚎妫€鏌ワ紙瑙嗚窛澶栦笉鍙兘琚湅瑙侊紝
    /// 妫€鏌ュ繀鐒跺叏閮ㄨ惤绌猴級銆傚綋鍓嶅彲瑙佺殑鏁屼汉涓嶈烦杩囷紝閬垮厤鐘舵€佹粸鐣欍€?    /// 璇佹嵁锛歟xternal/decompile-cache/eft-0.16-spt3114/EnemyInfo.cs:494-571
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
                // fail-open锛氳ˉ涓佸紓甯告椂鏀捐鍘熼€昏緫
            }
            return true;
        }
    }
}

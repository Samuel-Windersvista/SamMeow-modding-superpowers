using System.Reflection;
using EFT;
using HarmonyLib;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P5: AddEnemy 鍒濆鎰熺煡璺濈闂ㄩ檺銆?    /// 鍘熺増锛歜ot 婵€娲绘椂鎶婃垬鍖哄唴鎵€鏈夌帺瀹剁櫥璁拌繘 EnemyInfos锛坥nActivation=true锛夛紝璺濈鏃犱笂闄愶紝
    /// 瀵艰嚧鍏ㄥ浘 bot 瀵圭帺瀹舵寔鏈夎蹇嗗苟鍛ㄦ湡鎬у仛鎰熺煡灏勭嚎銆?    /// 琛ヤ竵锛氫粎闂ㄩ檺 onActivation 鐨勫垵濮嬬櫥璁帮紱鏋０/鍙楀嚮/缁勫唴閫氭姤绛変簨浠堕┍鍔ㄧ櫥璁帮紙onActivation=false锛?    /// 涓嶅彈褰卞搷 鈥斺€?杩滃鐜╁涓€鏃﹀紑鐏垨琚叾浠?bot 鐩嚮浠嶄細姝ｅ父杩涘叆鎰熺煡銆?    /// 璇佹嵁锛歟xternal/decompile-cache/eft-0.16-spt3114/BotMemoryClass.cs:769-799
    /// </summary>
    internal static class AddEnemyDistanceGatePatch
    {
        private static readonly AccessTools.FieldRef<BotMemoryClass, BotOwner> OwnerRef =
            AccessTools.FieldRefAccess<BotMemoryClass, BotOwner>("botOwner_0");

        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BotMemoryClass), "AddEnemy");
        }

        // ReSharper disable InconsistentNaming
        public static bool Prefix(BotMemoryClass __instance, IPlayer enemy, bool onActivation)
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
                BotOwner owner = OwnerRef(__instance);
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

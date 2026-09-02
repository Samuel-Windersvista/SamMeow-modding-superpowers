using acidphantasm_accessibilityindicators.Helpers;
using acidphantasm_accessibilityindicators.IndicatorUI;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace acidphantasm_accessibilityindicators.Patches
{
    internal class PhraseSpeakerClassPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(PhraseSpeakerClass), nameof(PhraseSpeakerClass.Play));
        }

        [PatchPostfix]
        static void PatchPostfix(PhraseSpeakerClass __instance, EPhraseTrigger trigger, bool demand)
        {
            Player player = Utils.GetProfileByID(__instance.Id);

            if (player == null
                || player.IsYourPlayer
                || System.Enum.IsDefined(typeof(BannedPhrases), trigger.ToString())
                || !Indicators.enable
                || !Indicators.enableVoicelines
                || (!player.IsAI && Utils.IsGroupedWithMainPlayer(player) && !Indicators.showTeammates)) return;

            bool isTeammate = Utils.IsGroupedWithMainPlayer(player);
            Indicators.PrepareVoice(player.Position, player.ProfileId, isTeammate);

        }
    }
}

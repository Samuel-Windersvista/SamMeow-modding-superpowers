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
            return AccessTools.Method(typeof(BaseSpeaker), nameof(BaseSpeaker.Play));
        }

        [PatchPostfix]
        static void PatchPostfix(BaseSpeaker __instance, EPhraseTrigger trigger, bool demand)
        {
            Player player = ModUtils.GetProfileByID(__instance.Id);

            if (player == null
                || player.IsYourPlayer
                || System.Enum.IsDefined(typeof(BannedPhrases), trigger.ToString())
                || !Indicators.enable
                || !Indicators.enableVoicelines
                || (!player.IsAI && ModUtils.IsGroupedWithMainPlayer(player) && !Indicators.showTeammates)) return;

            bool isTeammate = ModUtils.IsGroupedWithMainPlayer(player);
            Indicators.PrepareVoice(player.Position, player.ProfileId, isTeammate);

        }
    }
}

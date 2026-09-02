using acidphantasm_accessibilityindicators.Helpers;
using acidphantasm_accessibilityindicators.IndicatorUI;
using Audio.Data;
using CommonAssets.Scripts.Audio;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;

namespace acidphantasm_accessibilityindicators.Patches
{
    internal class PlayerDefaultPlayPatch : ModulePatch
    {
        private static FieldInfo distanceInfo;
        protected override MethodBase GetTargetMethod()
        {
            distanceInfo = AccessTools.Field(typeof(Player), "_distance");
            return AccessTools.Method(typeof(Player), nameof(Player.DefaultPlay));
        }

        [PatchPostfix]
        static void PatchPostfix(Player __instance, EAudioMovementState movementState)
        {
            if (__instance == null 
                || __instance.IsYourPlayer 
                || !Indicators.enable
                || (!__instance.IsAI && Utils.IsGroupedWithMainPlayer(__instance) && !Indicators.showTeammates)) return;

            Vector3 position = __instance.Position;
            float distance = (float)distanceInfo.GetValue(__instance);
            bool isTeammate = Utils.IsGroupedWithMainPlayer(__instance);

            Indicators.PrepareStep(movementState, position, distance, __instance.ProfileId, isTeammate);
        }
    }
    internal class PlayerPlayStepSoundPatch : ModulePatch
    {
        private static FieldInfo distanceInfo;
        protected override MethodBase GetTargetMethod()
        {
            distanceInfo = AccessTools.Field(typeof(Player), "_distance");
            return AccessTools.Method(typeof(Player), nameof(Player.PlayStepSound));
        }

        [PatchPostfix]
        static void PatchPostfix(Player __instance)
        {
            if (__instance == null
                || __instance.IsYourPlayer
                || !Indicators.enable
                || (!__instance.IsAI && Utils.IsGroupedWithMainPlayer(__instance) && !Indicators.showTeammates)) return;

            Vector3 position = __instance.Position;
            float distance = (float)distanceInfo.GetValue(__instance);
            EAudioMovementState eaudioMovementState = ((__instance.Pose == EPlayerPose.Duck) ? EAudioMovementState.Duck : EAudioMovementState.Run);
            bool isTeammate = Utils.IsGroupedWithMainPlayer(__instance);

            Indicators.PrepareStep(eaudioMovementState, position, distance, __instance.ProfileId, isTeammate);
        }
    }
    internal class PlayerMethod61Patch : ModulePatch
    {
        private static FieldInfo distanceInfo;
        protected override MethodBase GetTargetMethod()
        {
            distanceInfo = AccessTools.Field(typeof(Player), "_distance");
            return AccessTools.Method(typeof(Player), nameof(Player.method_61));
        }

        [PatchPostfix]
        static void PatchPostfix(Player __instance)
        {
            if (__instance == null
                || __instance.IsYourPlayer
                || !Indicators.enable
                || (!__instance.IsAI && Utils.IsGroupedWithMainPlayer(__instance) && !Indicators.showTeammates)) return;

            if (__instance.CurrentState.Name is EPlayerState.Sprint)
            {
                Vector3 position = __instance.Position;
                float distance = (float)distanceInfo.GetValue(__instance);
                var movementState = EAudioMovementState.Sprint;
                bool isTeammate = Utils.IsGroupedWithMainPlayer(__instance);
                Indicators.PrepareStep(movementState, position, distance, __instance.ProfileId, isTeammate);
            }
        }
    }
}

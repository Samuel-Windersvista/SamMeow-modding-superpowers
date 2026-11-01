using System.Reflection;
using EFT.Interactive;
using DoorOverhaul.Doors;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace DoorOverhaul.Patches
{
    internal class PartialOpenSwingPatch : ModulePatch
    {
        internal sealed class ActiveOverride
        {
            public OpenOverride Override;
            public float OriginalOpenAngle;
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.InitializeSmoothOpenInteraction),
                new[] { typeof(EDoorState), typeof(bool) });
        }

        [PatchPrefix]
        private static void Prefix(WorldInteractiveObject __instance, EDoorState state, out ActiveOverride __state)
        {
            __state = null;
            if (state != EDoorState.Open || !DoorOpenOverrides.TryConsume(__instance, out OpenOverride armed))
            {
                return;
            }

            __state = new ActiveOverride { Override = armed, OriginalOpenAngle = __instance.OpenAngle };
            __instance.OpenAngle = DoorMath.AngleAt(__instance, armed.Fraction);
        }

        [PatchFinalizer]
        private static void Finalizer(WorldInteractiveObject __instance, ActiveOverride __state, WorldInteractiveObject.InteractionState ____interaction)
        {
            if (__state == null)
            {
                return;
            }

            __instance.OpenAngle = __state.OriginalOpenAngle;

            if (Mathf.Abs(__state.Override.Slowdown - 1f) > 0.01f && ____interaction != null)
            {
                AnimationCurve curve = Stretch(____interaction.ProgressCurve, __state.Override.Slowdown);
                ____interaction.ProgressCurve = curve;
                ____interaction.Duration = curve.length > 0 ? curve[curve.length - 1].time : 0f;
            }
        }

        private static AnimationCurve Stretch(AnimationCurve source, float factor)
        {
            Keyframe[] keys = source.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time *= factor;
                keys[i].inTangent /= factor;
                keys[i].outTangent /= factor;
            }

            return new AnimationCurve(keys)
            {
                preWrapMode = source.preWrapMode,
                postWrapMode = source.postWrapMode
            };
        }
    }
}

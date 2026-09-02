using System.Reflection;
using SPT.Reflection.Patching;
using EFT.UI;
using HarmonyLib;
using UnityEngine;
using EFT.UI.WeaponModding;

namespace TraderModding
{
    public class ItemObserveScreenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemObserveScreen<EditBuildScreen.EditBuildScreenController, EditBuildScreen>), nameof(ItemObserveScreen<EditBuildScreen.EditBuildScreenController, EditBuildScreen>.Update));
        }

        [PatchPostfix]
        public static void Postfix(ItemObserveScreen<EditBuildScreen.EditBuildScreenController, EditBuildScreen> __instance)
        {
            if (__instance == null)
                return;

            if (Globals.isOnModdingScreen)
            {
                WeaponPreview wp = __instance._weaponPreview;
                if (wp != null)
                {
                    if (wp.WeaponPreviewCamera == null)
                        return;

                    Transform transform = wp.WeaponPreviewCamera.transform;

                    if (transform == null)
                        return;

                    if (Input.mouseScrollDelta.y != 0)
                    {
                        float zoomAmount = Input.mouseScrollDelta.y * 0.1f;
                        if (transform.position.z + zoomAmount < -0.05)
                        {
                            transform.Translate(new Vector3(0f, 0f, zoomAmount));
                            __instance.UpdatePositions();
                        }
                    }
                }
            }
        }
    }


    public class ItemObserveScreenRefreshIconsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemObserveScreen<EditBuildScreen.EditBuildScreenController, EditBuildScreen>), nameof(ItemObserveScreen<EditBuildScreen.EditBuildScreenController, EditBuildScreen>.CreateModSlotViews));
        }

        [PatchPostfix]
        static void Postfix()
        {
            TraderModdingUtils.UpdateBuildCost();
        }
    }
}

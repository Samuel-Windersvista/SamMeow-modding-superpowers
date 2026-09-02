using System.Reflection;
using EFT;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.Matchmaker;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace _DisableScavMode_egboggied.Patches;

public class ScavModePatch : ModulePatch {
    protected static Vector3 OldPmcPos { get; set; } = Vector3.zero;

    protected static bool IsPosSaved() {
        return OldPmcPos != Vector3.zero;
    }

    protected override MethodBase GetTargetMethod() {
        return typeof(MatchMakerSideSelectionScreen).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance,
                                                               null,
                                                               [
                                                                   typeof(ISession), typeof(RaidSettings),
                                                                   typeof(IHealthController),
                                                                   typeof(InventoryController)
                                                               ], null);
    }

    // disabling scav mode must happen in postfix after everything is called in the original method
    [PatchPostfix]
    public static void Postfix(MatchMakerSideSelectionScreen __instance, AddViewListClass ___UI,
                               PlayerModelView ____savageModelView, Button ____savagesBigButton,
                               Button ____pmcBigButton, UIAnimatedToggleSpawner ____savagesButton,
                               ref ESideType ___esideType_0) {
        if (Plugin.ScavMode.Value) return;

        // if scav was selected before disabling scav mode, the side is still set to Savage and makes the Pmc button not selected
        // it's possible to still enter raid as a scav even with all the ui elements disabled
        // so manually set to pmc
        if (___esideType_0 == ESideType.Savage) ___esideType_0 = ESideType.Pmc;

        // hide savage model
        ____savageModelView.Dispose();
        ___UI.DisposeReference(ref ____savageModelView);

        // remove button listeners
        ____savagesBigButton.onClick.RemoveListener(__instance.method_16);
        ____savagesButton.SpawnedObject.onValueChanged.RemoveListener(__instance.method_14);

        // hide button itself
        ____savagesButton.transform.gameObject.SetActive(false);

        ____pmcBigButton.transform.parent.transform.localPosition = new Vector3(-220, 520, 0);
    }

    // re-enabling scav mode must happen in prefix before the original method is called again
    [PatchPrefix]
    public static bool Prefix(MatchMakerSideSelectionScreen __instance, Button ____savagesBigButton,
                              Button ____pmcBigButton, UIAnimatedToggleSpawner ____savagesButton) {
        // store the original pmc position before we do anything
        if (!IsPosSaved()) OldPmcPos = ____pmcBigButton.transform.parent.transform.localPosition;

        if (!Plugin.ScavMode.Value) return true;

        // only reset everything back to default if scav mode is enabled and it's in a previously disabled state
        if (!____savagesButton.transform.gameObject.activeSelf) {
            // re add button listeners
            ____savagesBigButton.onClick.AddListener(__instance.method_16);
            ____savagesButton.SpawnedObject.onValueChanged.AddListener(__instance.method_14);

            // show button
            ____savagesButton.transform.gameObject.SetActive(true);

            // restore original position
            ____pmcBigButton.transform.parent.transform.localPosition = OldPmcPos;
        }

        return true;
    }
}
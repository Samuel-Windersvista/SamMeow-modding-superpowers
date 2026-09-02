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
        // PORT-NOTE: 3.11 ISession → 4.1 EFT.IEftSession（签名精确匹配 4.1 Show 重载）
        return typeof(MatchMakerSideSelectionScreen).GetMethod("Show", BindingFlags.Public | BindingFlags.Instance,
                                                               null,
                                                               [
                                                                   typeof(IEftSession), typeof(RaidSettings),
                                                                   typeof(IHealthController),
                                                                   typeof(InventoryController)
                                                               ], null);
    }

    // disabling scav mode must happen in postfix after everything is called in the original method
    [PatchPostfix]
    public static void Postfix(MatchMakerSideSelectionScreen __instance, PlayerModelView _savageModelView,
                               Button _savagesBigButton, Button _pmcBigButton,
                               UIAnimatedToggleSpawner _savagesButton, ref ESideType _sideType) {
        if (Plugin.ScavMode.Value) return;

        // if scav was selected before disabling scav mode, the side is still set to Savage and makes the Pmc button not selected
        // it's possible to still enter raid as a scav even with all the ui elements disabled
        // so manually set to pmc
        if (_sideType == ESideType.Savage) _sideType = ESideType.Pmc;

        // hide savage model
        _savageModelView.Dispose();
        // PORT-NOTE: 3.11 的 AddViewListClass ___UI 类型在 4.1 已移除（AddViewListClass 不存在），
        // 其 DisposeReference(ref ...) 引用清理由 PlayerModelView.Dispose()（UIElement.Dispose，公开）承担，故删除该行。
        // 4.1 字段：esideType_0→_sideType、savageModelView→_savageModelView、savagesBigButton→_savagesBigButton、
        // pmcBigButton→_pmcBigButton、savagesButton→_savagesButton（均由 4.1 Assembly-CSharp 字段表核对）。

        // remove button listeners
        // PORT-NOTE: 3.11 监听方法 method_16 / method_14 → 4.1 由 Awake() 的 ldftn 确认：
        // _savagesBigButton.onClick 挂 CG_Awake3、_savagesButton.SpawnedObject.onValueChanged 挂 CG_Awake1
        _savagesBigButton.onClick.RemoveListener(__instance.CG_Awake3);
        _savagesButton.SpawnedObject.onValueChanged.RemoveListener(__instance.CG_Awake1);

        // hide button itself
        _savagesButton.transform.gameObject.SetActive(false);

        _pmcBigButton.transform.parent.transform.localPosition = new Vector3(-220, 520, 0);
    }

    // re-enabling scav mode must happen in prefix before the original method is called again
    [PatchPrefix]
    public static bool Prefix(MatchMakerSideSelectionScreen __instance, Button _savagesBigButton,
                              Button _pmcBigButton, UIAnimatedToggleSpawner _savagesButton) {
        // store the original pmc position before we do anything
        if (!IsPosSaved()) OldPmcPos = _pmcBigButton.transform.parent.transform.localPosition;

        if (!Plugin.ScavMode.Value) return true;

        // only reset everything back to default if scav mode is enabled and it's in a previously disabled state
        if (!_savagesButton.transform.gameObject.activeSelf) {
            // re add button listeners
            _savagesBigButton.onClick.AddListener(__instance.CG_Awake3);
            _savagesButton.SpawnedObject.onValueChanged.AddListener(__instance.CG_Awake1);

            // show button
            _savagesButton.transform.gameObject.SetActive(true);

            // restore original position
            _pmcBigButton.transform.parent.transform.localPosition = OldPmcPos;
        }

        return true;
    }
}

using System;
using System.Reflection;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace UIFixes;

public static class FocusFleaOfferNumberPatches
{
    public static void Enable()
    {
        new MoneyPatch().Enable();
        new BarterPatch().Enable();
    }

    public class MoneyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(HandoverRagfairMoneyWindow), nameof(HandoverRagfairMoneyWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(HandoverRagfairMoneyWindow __instance, TMP_InputField ____inputField)
        {
            // 4.1.2: "fill all" callback (3.x method_11) removed; keyboard focus kept

            ____inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            ____inputField.ActivateInputField();
            ____inputField.Select();
        }
    }

    public class BarterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(HandoverExchangeableItemsWindow), nameof(HandoverExchangeableItemsWindow.Show));
        }

        [PatchPostfix]
        public static void Postfix(HandoverExchangeableItemsWindow __instance, TMP_InputField ____inputField)
        {
            // 4.1.2: "fill all" callback (3.x method_15) removed; keyboard focus kept

            ____inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            ____inputField.ActivateInputField();
            ____inputField.Select();
        }
    }

    public class AllButtonKeybind : MonoBehaviour
    {
        private Action _purchaseAllAction;

        public void Init(Action purchaseAllAction)
        {
            _purchaseAllAction = purchaseAllAction;
        }

        public void Update()
        {
            if (Settings.PurchaseAllKeybind.Value.IsDown())
            {
                _purchaseAllAction();
            }
        }
    }
}
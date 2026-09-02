using EFT;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace hideoutcat.bepinex
{
    internal class PatchBonusPanelUpdateView : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BonusPanel), nameof(BonusPanel.UpdateView));
        }

        // 3.11: BonusPanel had a SkillBonusAbstractClass field; 4.1 renamed it to EFT.Bonus _bonus
        // Harmony field injection: "____bonus" = instance field "_bonus"
        [PatchPostfix]
        private static void PatchPostfix(Bonus ____bonus, TextMeshProUGUI ____description, TextMeshProUGUI ____effect, Image ____icon)
        {
            if (____bonus.Id.ToString() != "64f5b9e5fa34f11b380756d6")
                return;

            //____icon.sprite = sprite;
            ____description.text = "Unlocks cat";
            ____effect.text = "";
        }
    }
}

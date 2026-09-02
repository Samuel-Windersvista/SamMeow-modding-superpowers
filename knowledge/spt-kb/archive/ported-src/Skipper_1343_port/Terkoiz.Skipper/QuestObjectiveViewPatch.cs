using System.Reflection;
using SPT.Reflection.Patching;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace Terkoiz.Skipper
{
    public class QuestObjectiveViewPatch : ModulePatch
    {
        internal static GameObject LastSeenObjectivesBlock;

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(QuestObjectiveView), nameof(QuestObjectiveView.Show));
        }

        [PatchPostfix]
        private static void PatchPostfix(DefaultUIButton ____handoverButton, QuestController questController, Condition condition, Quest quest, QuestObjectiveView __instance)
        {
            if (!SkipperPlugin.ModEnabled.Value)
            {
                return;
            }

            // The handover button is usually only missing in the non-trader task view screens, where we don't want to allow skipping either way
            if (____handoverButton == null)
            {
                return;
            }

            LastSeenObjectivesBlock = __instance.transform.parent.gameObject;

            var skipButton = Object.Instantiate(____handoverButton, ____handoverButton.transform.parent.transform);

            skipButton.SetRawText("SKIP", 22);
            skipButton.gameObject.name = SkipperPlugin.SkipButtonName;
            skipButton.gameObject.GetComponent<UnityEngine.UI.LayoutElement>().minWidth = 100f;
            skipButton.gameObject.SetActive(SkipperPlugin.AlwaysDisplay.Value && !quest.IsConditionDone(condition));

            skipButton.OnClick.RemoveAllListeners();
            skipButton.OnClick.AddListener(() => ItemUiContext.Instance.ShowMessageWindow(
                description: "Are you sure you want to autocomplete this quest objective?",
                acceptAction: () =>
                {
                    if (quest.IsConditionDone(condition))
                    {
                        skipButton.gameObject.SetActive(false);
                        return;
                    }

                    SkipperPlugin.Logger.LogDebug($"Setting condition {condition.id} value to {condition.value}");

                    // This line will force any condition checker to pass, as the 'condition.value' field contains the "goal" of any quest condition
                    quest.ProgressCheckers[condition].SetCurrentValueGetter(_ => condition.value);

                    // PORT-NOTE: SPT 4.1.2 is deobfuscated, so the 3.11-era reflection heuristic
                    // (locating the concrete quest controller type by the 'OnConditionQuestTimeExpired'
                    // event, then reading a '<typename>_0' field) is obsolete. The quest controller now
                    // exposes the conditions manager via the public 'ConditionsConnectorsManager' property,
                    // and 'SetConditionCurrentValue' is a public (abstract) method on
                    // ConditionsConnectorsManagerClient<T>. Runtime instance is
                    // ConditionsConnectorsManagerQuestClientGame/Backend<Quest>, which implements it.
                    var conditionsManager = (ConditionsConnectorsManagerClient<Quest>)questController.ConditionsConnectorsManager;
                    conditionsManager.SetConditionCurrentValue(quest, EQuestStatus.AvailableForFinish, condition, condition.value, true);

                    skipButton.gameObject.SetActive(false);
                },
                cancelAction: () => { },
                caption: "Confirmation"));
        }
    }
}

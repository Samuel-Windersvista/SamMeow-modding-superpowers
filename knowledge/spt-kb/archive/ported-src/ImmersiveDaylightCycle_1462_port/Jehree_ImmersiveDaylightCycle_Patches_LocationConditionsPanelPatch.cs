using System;
using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using Jehree.ImmersiveDaylightCycle.Helpers;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace Jehree.ImmersiveDaylightCycle.Patches;

internal class LocationConditionsPanelPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.FirstMethod(typeof(LocationConditionsPanel), (Func<MethodInfo, bool>)((MethodInfo x) => x.Name == "Set" && x.GetParameters()[0].Name == "session"));
	}

	[PatchPostfix]
	private static void Postfix(RaidSettings raidSettings, bool takeFromCurrent, MatchMakerAcceptScreen __instance)
	{
		if (!Settings.ModEnabled.Value)
		{
			return;
		}
		DateTime currentTime = ModUtils.GetCurrentTime();
		TextMeshProUGUI component;
		try
		{
			component = ((Component)((Component)((Component)__instance).transform.Find("TimePanel")).gameObject.transform.Find("Time")).gameObject.GetComponent<TextMeshProUGUI>();
		}
		catch (Exception)
		{
			return;
		}
		if (raidSettings.SelectedLocation.Id == "factory4_day" || raidSettings.SelectedLocation.Id == "factory4_night")
		{
			if (!Settings.FactoryTimeAlwaysSelectable.Value)
			{
				if (ModUtils.IsDayTime(currentTime))
				{
					SetTimePanelText(component, "15:28:00");
				}
				else
				{
					SetTimePanelText(component, "03:28:00");
				}
			}
		}
		else
		{
			SetTimePanelText(component, currentTime.ToString("HH:mm:ss"));
		}
	}

	private static void SetTimePanelText(TextMeshProUGUI timePanel, string text)
	{
		try
		{
			((TMP_Text)timePanel).text = text;
		}
		catch (Exception)
		{
		}
	}
}

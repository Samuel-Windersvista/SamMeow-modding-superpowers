using System;
using System.Reflection;
using EFT;
using EFT.UI.Matchmaker;
using HarmonyLib;
using Jehree.ImmersiveDaylightCycle.Helpers;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine.UI;

namespace Jehree.ImmersiveDaylightCycle.Patches;

internal class TimeUIPanelPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(LocationConditionsPanel), "Set", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	private static void Postfix(RaidSettings raidSettings, bool takeFromCurrent, ref TextMeshProUGUI ____currentPhaseTime, ref TextMeshProUGUI ____nextPhaseTime, ref Toggle ____pmTimeToggle, ref Toggle ____amTimeToggle)
	{
		if (!Settings.ModEnabled.Value)
		{
			ModUtils.EnableTimeUI(____nextPhaseTime, ____pmTimeToggle, "03:28:00", chooseThisTime: false);
			ModUtils.EnableTimeUI(____currentPhaseTime, ____amTimeToggle, "15:28:00", chooseThisTime: false);
			return;
		}
		DateTime currentTime = ModUtils.GetCurrentTime();
		if (raidSettings.SelectedLocation.Id == "factory4_day" || raidSettings.SelectedLocation.Id == "factory4_night")
		{
			if (Settings.FactoryTimeAlwaysSelectable.Value)
			{
				ModUtils.EnableTimeUI(____nextPhaseTime, ____pmTimeToggle, "NIGHT-" + currentTime.ToString("HH"), chooseThisTime: false);
				ModUtils.EnableTimeUI(____currentPhaseTime, ____amTimeToggle, "DAY-" + currentTime.ToString("HH"), chooseThisTime: false);
			}
			else if (ModUtils.IsDayTime(currentTime))
			{
				ModUtils.DisableTimeUI(____nextPhaseTime, ____pmTimeToggle);
				ModUtils.EnableTimeUI(____currentPhaseTime, ____amTimeToggle, "DAY-" + currentTime.ToString("HH"), chooseThisTime: false);
			}
			else
			{
				ModUtils.DisableTimeUI(____currentPhaseTime, ____amTimeToggle);
				ModUtils.EnableTimeUI(____nextPhaseTime, ____pmTimeToggle, "NIGHT-" + currentTime.ToString("HH"), chooseThisTime: false);
			}
		}
		else
		{
			ModUtils.DisableTimeUI(____nextPhaseTime, ____pmTimeToggle);
			ModUtils.EnableTimeUI(____currentPhaseTime, ____amTimeToggle, currentTime.ToString("HH:mm:ss"));
		}
	}
}

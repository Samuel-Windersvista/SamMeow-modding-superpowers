using System;
using System.Reflection;
using EFT.UI.Matchmaker;
using HarmonyLib;
using Jehree.ImmersiveDaylightCycle.Helpers;
using SPT.Reflection.Patching;

namespace Jehree.ImmersiveDaylightCycle.Patches;

internal class TimeUIUpdatePatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(LocationConditionsPanel), "Update", (Type[])null, (Type[])null);
	}

	[PatchPrefix]
	private static bool Prefix()
	{
		if (!Settings.ModEnabled.Value)
		{
			return true;
		}
		return false;
	}
}

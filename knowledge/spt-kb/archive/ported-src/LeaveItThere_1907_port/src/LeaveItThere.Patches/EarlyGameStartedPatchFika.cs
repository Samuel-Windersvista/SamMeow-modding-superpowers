using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using LeaveItThere.Components;
using LeaveItThere.CustomUI;
using SPT.Reflection.Patching;

namespace LeaveItThere.Patches;

internal class EarlyGameStartedPatchFika : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(GameWorld), "RegisterRestrictableZones", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	private static void PatchPrefix(GameWorld __instance)
	{
		if (!(__instance is HideoutGameWorld))
		{
			LITSession.CreateNewModSession();
			MoveModeUI.Instance.SetActive(isActive: false);
		}
	}
}

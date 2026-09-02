using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using LeaveItThere.Components;
using SPT.Reflection.Patching;

namespace LeaveItThere.Patches;

internal class EarlyGameStartedPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(BotsController), "SetSettings", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	private static void PatchPrefix()
	{
		LITSession.CreateNewModSession();
	}
}

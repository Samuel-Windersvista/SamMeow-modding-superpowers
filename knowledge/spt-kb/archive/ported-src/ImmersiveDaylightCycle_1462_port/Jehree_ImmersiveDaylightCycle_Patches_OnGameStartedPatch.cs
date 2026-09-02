using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using ImmersiveDaylightCycle.Fika;
using Jehree.ImmersiveDaylightCycle.Helpers;
using SPT.Reflection.Patching;

namespace Jehree.ImmersiveDaylightCycle.Patches;

internal class OnGameStartedPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(GameWorld), "OnGameStarted", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	private static void Postfix()
	{
		if (Settings.ModEnabled.Value)
		{
			if (FikaBridge.IAmHost())
			{
				ModUtils.ServerRoute(ModUtils.HostRaidStartedURL, FikaBridge.GetRaidId());
			}
			ModUtils.SetRaidTime();
		}
	}
}

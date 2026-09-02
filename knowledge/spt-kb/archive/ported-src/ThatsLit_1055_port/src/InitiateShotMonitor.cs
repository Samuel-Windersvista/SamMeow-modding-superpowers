using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace ThatsLit.Patches.Vision;

public class InitiateShotMonitor : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(EFT.Player.FirearmController), "InitiateShot", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	public static void PatchPostfix(EFT.Player.FirearmController __instance, Vector3 shotDirection, ref Player ____player)
	{
		ThatsLitPlayer value = null;
		Singleton<ThatsLitGameworld>.Instance?.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)____player, out value);
		if (!((UnityEngine.Object)(object)value == (UnityEngine.Object)null))
		{
			value.lastShotVector = shotDirection;
			value.lastShotTime = Time.time;
		}
	}
}

public class ClientInitiateShotMonitor : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(EFT.ClientFirearmController), "InitiateShot", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	public static void PatchPostfix(EFT.ClientFirearmController __instance, Vector3 shotDirection, ref Player ____player)
	{
		ThatsLitPlayer value = null;
		Singleton<ThatsLitGameworld>.Instance?.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)____player, out value);
		if (!((UnityEngine.Object)(object)value == (UnityEngine.Object)null))
		{
			value.lastShotVector = shotDirection;
			value.lastShotTime = Time.time;
		}
	}
}

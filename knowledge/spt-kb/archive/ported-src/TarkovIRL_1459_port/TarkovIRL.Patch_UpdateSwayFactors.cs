using System.Reflection;
using EFT;
using EFT.Animations;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace TarkovIRL;

internal class Patch_UpdateSwayFactors : ModulePatch
{
	private static FieldInfo playerField;

	private static FieldInfo fcField;

	protected override MethodBase GetTargetMethod()
	{
		playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
		fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
		return typeof(ProceduralWeaponAnimation).GetMethod("UpdateSwayFactors", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void Postfix(ProceduralWeaponAnimation __instance)
	{
		if (__instance == null || !PrimeMover.IsWeaponSway.Value || SwayController.IsSwayUpdatedThisFrame)
		{
			return;
		}
		Player.FirearmController firearmController = (Player.FirearmController)fcField.GetValue(__instance);
		if (!(firearmController == null))
		{
			Player player = (Player)playerField.GetValue(firearmController);
			if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
			{
				Vector3 newSway = SwayController.GetNewSway(__instance.MotionReact.SwayFactors, __instance.IsAiming);
				__instance.MotionReact.SwayFactors = newSway;
				SwayController.IsSwayUpdatedThisFrame = true;
			}
		}
	}
}

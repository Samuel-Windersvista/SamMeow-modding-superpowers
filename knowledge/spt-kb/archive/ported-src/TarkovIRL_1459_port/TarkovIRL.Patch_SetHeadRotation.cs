using System.Reflection;
using EFT;
using EFT.Animations;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace TarkovIRL;

public class Patch_SetHeadRotation : ModulePatch
{
	private static FieldInfo _playerField;

	private static FieldInfo _fcField;

	private static Vector3 _dzLerp = Vector3.zero;

	private static Vector3 _dzLerpTarget = Vector3.zero;

	protected override MethodBase GetTargetMethod()
	{
		_playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
		_fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
		return typeof(ProceduralWeaponAnimation).GetMethod("SetHeadRotation", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPrefix]
	private static bool Prefix(ProceduralWeaponAnimation __instance, Vector3 headRot)
	{
		if (__instance == null)
		{
			return true;
		}
		Player.FirearmController firearmController = (Player.FirearmController)_fcField.GetValue(__instance);
		if (firearmController == null)
		{
			_dzLerpTarget = headRot;
			return true;
		}
		Player player = (Player)_playerField.GetValue(firearmController);
		if (player != null && player.IsYourPlayer && player.MovementContext.CurrentState.Name != EPlayerState.Stationary)
		{
			Vector3 vector = headRot;
			if (PrimeMover.IsWeaponDeadzone.Value)
			{
				vector = NewDeadzoneController.GetHeadRotWithDeadzone(vector);
			}
			vector.y *= 1.5f;
			player.HeadRotation = vector;
			AccessTools.Field(typeof(ProceduralWeaponAnimation), "_headRotationVec").SetValue(__instance, vector);
			return false;
		}
		return true;
	}
}

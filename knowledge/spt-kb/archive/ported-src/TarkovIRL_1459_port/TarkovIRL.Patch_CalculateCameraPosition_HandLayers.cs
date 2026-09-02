using System.Reflection;
using EFT;
using EFT.Animations;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace TarkovIRL;

internal class Patch_CalculateCameraPosition_HandLayers : ModulePatch
{
	private static FieldInfo playerField;

	private static FieldInfo fcField;

	protected override MethodBase GetTargetMethod()
	{
		playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
		fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
		return typeof(ProceduralWeaponAnimation).GetMethod("CalculateCameraPosition", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(ProceduralWeaponAnimation __instance)
	{
		if (__instance == null)
		{
			return;
		}
		Player.FirearmController firearmController = (Player.FirearmController)fcField.GetValue(__instance);
		if (firearmController == null)
		{
			return;
		}
		Player player = (Player)playerField.GetValue(firearmController);
		if (!(player != null) || !player.IsYourPlayer)
		{
			return;
		}
		WeaponController.IsUsingMounted = __instance.IsMountedState;
		if (!AnimStateController.IsBlindfire && !WeaponController.IsUsingMounted)
		{
			EfficiencyController.UpdateEfficiency(player);
			Vector3 modifiedHandPosForBreath = HandBreathController.GetModifiedHandPosForBreath(player);
			Vector3 handsShakePosition = HandShakeController.GetHandsShakePosition(player);
			Vector3 modifiedHandPosWithPose = HandPoseController.GetModifiedHandPosWithPose(player);
			Quaternion modifiedHandRotWithPoseChange = HandPoseController.GetModifiedHandRotWithPoseChange();
			Vector3 modifiedHandPosWithPoseChange = HandPoseController.GetModifiedHandPosWithPoseChange(player);
			Vector3 modifiedHandPosZMovement = HandMovWithRotController.GetModifiedHandPosZMovement(player);
			Vector3 modifiedHandPosForLoweredWeapon = HandMovWithRotController.GetModifiedHandPosForLoweredWeapon(player);
			Vector3 getModifiedHandPosFootstep = FootstepController.GetModifiedHandPosFootstep;
			Vector3 position = __instance.HandsContainer.WeaponRoot.localPosition;
			Quaternion rotation = __instance.HandsContainer.WeaponRoot.localRotation;
			ParallaxController.GetModifiedHandPosRotParallax(player, ref position, ref rotation);
			Vector3 newSwayPosition = NewSwayController.GetNewSwayPosition();
			Quaternion newSwayRotation = NewSwayController.GetNewSwayRotation();
			Vector3 sideToSidePosition = FootstepController.GetSideToSidePosition();
			Quaternion sideToSideRotation = FootstepController.GetSideToSideRotation();
			bool value = PrimeMover.IsBreathingEffect.Value;
			bool value2 = PrimeMover.IsPoseEffect.Value;
			bool value3 = PrimeMover.IsPoseChangeEffect.Value;
			bool value4 = PrimeMover.IsArmShakeEffect.Value;
			bool value5 = PrimeMover.IsSmallMovementsEffect.Value;
			bool value6 = PrimeMover.IsFootstepEffect.Value;
			bool value7 = PrimeMover.IsParallaxEffect.Value;
			bool value8 = PrimeMover.IsWeaponSway.Value;
			if (value)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += modifiedHandPosForBreath;
			}
			if (value2)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += modifiedHandPosWithPose;
			}
			if (value3)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += modifiedHandPosWithPoseChange;
				__instance.HandsContainer.WeaponRoot.localRotation *= modifiedHandRotWithPoseChange;
			}
			if (value4)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += handsShakePosition;
			}
			if (value5)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += modifiedHandPosZMovement;
				__instance.HandsContainer.WeaponRoot.localPosition += modifiedHandPosForLoweredWeapon;
			}
			if (value7)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += position;
				__instance.HandsContainer.WeaponRoot.localRotation *= rotation;
			}
			if (value6)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += getModifiedHandPosFootstep;
				__instance.HandsContainer.WeaponRoot.localPosition += sideToSidePosition;
				__instance.HandsContainer.WeaponRoot.localRotation *= sideToSideRotation;
			}
			if (value8)
			{
				__instance.HandsContainer.WeaponRoot.localPosition += newSwayPosition;
				__instance.HandsContainer.WeaponRoot.localRotation *= newSwayRotation;
			}
			DirectionalSwayController.GetDirectionalSway(out var position2, out var rotation2);
			__instance.HandsContainer.WeaponRoot.localPosition += position2;
			__instance.HandsContainer.WeaponRoot.localRotation *= rotation2;
			WeaponSelectionController.GetWeaponSelectionTransforms(out var pos, out var rot);
			__instance.HandsContainer.WeaponRoot.localPosition += pos;
			__instance.HandsContainer.WeaponRoot.localRotation *= rot;
		}
	}
}

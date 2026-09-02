using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace TarkovIRL;

public class Patch_ProcessRotation : ModulePatch
{
	private static FieldInfo movementContextField;

	protected override MethodBase GetTargetMethod()
	{
		movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
		return typeof(MovementState).GetMethod("ProcessRotation", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPrefix]
	private static bool Prefix(MovementState __instance, float deltaTime)
	{
		MovementContext movementContext = (MovementContext)movementContextField.GetValue(__instance);
		TIRLUtils.LogError($"turn called, HandsToBodyAngle {movementContext.HandsToBodyAngle}, TrunkRotationLimit {movementContext.TrunkRotationLimit}");
		if (Mathf.Abs(movementContext.HandsToBodyAngle) > movementContext.TrunkRotationLimit * 0.2f)
		{
			__instance.ProcessUpperbodyRotation(deltaTime, false);
		}
		__instance.UpdateRotationSpeed(deltaTime);
		return false;
	}
}

using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace TarkovIRL;

internal class Patch_ProcessUpperbodyRotation : ModulePatch
{
	private static FieldInfo movementContextField;

	protected override MethodBase GetTargetMethod()
	{
		movementContextField = AccessTools.Field(typeof(MovementState), "MovementContext");
		return typeof(MovementState).GetMethod("ProcessUpperbodyRotation", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPrefix]
	private static bool Prefix(MovementState __instance, float deltaTime)
	{
		MovementContext movementContext = (MovementContext)movementContextField.GetValue(__instance);
		float y = Mathf.DeltaAngle((float)Math.Sign(movementContext.HandsToBodyAngle) * movementContext.TrunkRotationLimit, movementContext.HandsToBodyAngle);
		movementContext.ApplyRotation(Quaternion.Lerp(movementContext.TransformRotation, movementContext.TransformRotation * Quaternion.Euler(0f, y, 0f), 30f * PrimeMover.Instance.DeltaTime));
		return false;
	}
}

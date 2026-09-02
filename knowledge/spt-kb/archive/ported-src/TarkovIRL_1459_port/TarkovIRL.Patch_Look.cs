using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;

namespace TarkovIRL;

internal class Patch_Look : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(Player).GetMethod("Look", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void Postfix(Player __instance)
	{
		if (__instance != null && __instance.IsYourPlayer && __instance.MovementContext.CurrentState.Name != EPlayerState.Stationary && PrimeMover.IsSmallMovementsEffect.Value)
		{
			Vector3 headRotThisFrame = HeadRotController.GetHeadRotThisFrame(__instance.HeadRotation);
			__instance.HeadRotation = headRotThisFrame;
			__instance.ProceduralWeaponAnimation.SetHeadRotation(__instance.HeadRotation);
		}
	}
}

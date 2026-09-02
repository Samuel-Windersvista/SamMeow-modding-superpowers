using System.Reflection;
using EFT;
using EFT.Animations;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace TarkovIRL;

public class Patch_LerpCamera_ForceUpdateSway : ModulePatch
{
	private static FieldInfo playerField;

	private static FieldInfo fcField;

	protected override MethodBase GetTargetMethod()
	{
		playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
		fcField = AccessTools.Field(typeof(ProceduralWeaponAnimation), "_firearmController");
		return typeof(ProceduralWeaponAnimation).GetMethod("LerpCamera", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(ProceduralWeaponAnimation __instance, float dt)
	{
		if (__instance == null)
		{
			return;
		}
		Player.FirearmController firearmController = (Player.FirearmController)fcField.GetValue(__instance);
		if (!(firearmController == null))
		{
			Player player = (Player)playerField.GetValue(firearmController);
			AnimStateController.SetCurrentBodyAnimState(player.MovementContext.CurrentState.AnimatorStateHash);
			PlayerMotionController.UpdateMovementInformation(player);
			if (!player.IsInventoryOpened)
			{
				__instance.UpdateSwayFactors();
			}
		}
	}
}

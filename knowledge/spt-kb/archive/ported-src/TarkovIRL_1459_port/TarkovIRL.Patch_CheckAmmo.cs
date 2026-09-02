using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace TarkovIRL;

internal class Patch_CheckAmmo : ModulePatch
{
	private static FieldInfo playerField;

	protected override MethodBase GetTargetMethod()
	{
		playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
		return typeof(Player.FirearmController).GetMethod("CheckAmmo", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(Player.FirearmController __instance)
	{
		Player player = (Player)playerField.GetValue(__instance);
		if (player.IsYourPlayer)
		{
			AugmentedReloadController.RefreshAnimator(player.HandsAnimator);
		}
	}
}

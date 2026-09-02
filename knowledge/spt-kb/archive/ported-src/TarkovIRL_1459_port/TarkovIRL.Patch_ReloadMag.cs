using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace TarkovIRL;

public class Patch_ReloadMag : ModulePatch
{
	private static FieldInfo playerField;

	protected override MethodBase GetTargetMethod()
	{
		playerField = AccessTools.Field(typeof(Player.FirearmController), "_player");
		return typeof(Player.FirearmController).GetMethod("ReloadMag", BindingFlags.Instance | BindingFlags.Public);
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

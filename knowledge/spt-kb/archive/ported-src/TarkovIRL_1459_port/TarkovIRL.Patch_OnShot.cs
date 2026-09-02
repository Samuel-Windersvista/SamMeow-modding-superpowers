using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace TarkovIRL;

public class Patch_OnShot : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(Player).GetMethod("OnMakingShot", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(Player __instance)
	{
		if (__instance != null && __instance.IsYourPlayer)
		{
			Player.FirearmController firearmController = __instance.HandsController as Player.FirearmController;
			WeaponController.UpdateWpnStats(firearmController);
			if (WeaponController.HasCheekWeld() && firearmController.IsAiming)
			{
				ParallaxAdsController.StartNewShot(firearmController.Weapon);
			}
		}
	}
}

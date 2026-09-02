using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using TarkovIRL;

public class Patch_LateUpdate_UpdateWpnStats : ModulePatch
{
	private static int _weaponHashLastFrame;

	protected override MethodBase GetTargetMethod()
	{
		return typeof(Player).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(Player __instance)
	{
		Player.FirearmController firearmController = __instance.HandsController as Player.FirearmController;
		if (!(firearmController == null) && __instance.IsYourPlayer)
		{
			PlayerMotionController.UpdateMovementInformation(__instance);
			AnimStateController.SetCurrentWeaponAnimState(__instance.HandsAnimator.Animator.GetCurrentAnimatorStateInfo(1).nameHash);
			int hashCode = firearmController.Weapon.Name.GetHashCode();
			if (hashCode != _weaponHashLastFrame)
			{
				WeaponController.UpdateWpnStats(firearmController);
				WeaponController.SetCurrentWeaponHash(hashCode);
			}
			_weaponHashLastFrame = hashCode;
		}
	}
}

using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace TarkovIRL;

public class Patch_ThrowGrenade : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(Player).GetMethod("ThrowGrenade", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(Player __instance, bool lowThrow)
	{
		if (__instance != null && __instance.IsYourPlayer)
		{
			ThrowController.NewThrow(lowThrow);
		}
	}
}

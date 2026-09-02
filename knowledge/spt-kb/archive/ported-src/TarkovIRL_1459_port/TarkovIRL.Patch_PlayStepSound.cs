using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace TarkovIRL;

public class Patch_PlayStepSound : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(Player).GetMethod("PlayStepSound", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(Player __instance)
	{
		if (__instance != null && __instance.IsYourPlayer)
		{
			FootstepController.NewStep(__instance);
		}
	}
}

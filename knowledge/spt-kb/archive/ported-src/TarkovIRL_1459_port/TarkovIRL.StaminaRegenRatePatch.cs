using System.Reflection;
using SPT.Reflection.Patching;

namespace TarkovIRL;

public class StaminaRegenRatePatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(Physical).GetMethod("BaseStaminaRestorationFunc", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(Physical __instance, ref float __result)
	{
		float num = (PlayerMotionController.IsAugmentedBreath ? (-4f) : 0f);
		__result += num;
	}
}

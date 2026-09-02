using System.Reflection;
using SPT.Reflection.Patching;

namespace TarkovIRL;

internal class Patch_SetSprint : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(FirearmsAnimator).GetMethod("SetSprint", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPrefix]
	private static bool Prefix(FirearmsAnimator __instance, bool sprint)
	{
		TIRLUtils.LogError("set to sprint interrupted");
		return false;
	}
}

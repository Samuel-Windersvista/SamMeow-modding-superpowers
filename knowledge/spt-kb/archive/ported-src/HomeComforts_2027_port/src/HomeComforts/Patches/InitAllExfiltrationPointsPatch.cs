using System.Reflection;
using CommonAssets.Scripts.Game;
using SPT.Reflection.Patching;

namespace HomeComforts.Patches;

internal class InitAllExfiltrationPointsPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(ExfiltrationController).GetMethod("InitAllExfiltrationPoints");
	}

	[PatchPostfix]
	private static void Postfix(ref ExfiltrationController __instance)
	{
		SafehouseSession.InitializeCustomExfil(__instance);
	}
}

using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace HomeComforts.Patches;

public class GameStartedPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(GameWorld).GetMethod("OnGameStarted");
	}

	[PatchPrefix]
	private static void PatchPrefix()
	{
		// Vestigial: initialization is driven by LeaveItThere's early-game patch + LITStaticEvents.
	}
}

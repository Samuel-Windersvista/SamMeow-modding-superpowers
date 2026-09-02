using System.Reflection;
using EFT;
using LeaveItThere.Components;
using SPT.Reflection.Patching;

namespace LeaveItThere.Patches;

internal class InteractionsChangedHandlerPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return typeof(GamePlayerOwner).GetMethod("InteractionsChangedHandler");
	}

	[PatchPrefix]
	private static bool PatchPrefix()
	{
		return LITSession.Instance.InteractionsAllowed;
	}
}

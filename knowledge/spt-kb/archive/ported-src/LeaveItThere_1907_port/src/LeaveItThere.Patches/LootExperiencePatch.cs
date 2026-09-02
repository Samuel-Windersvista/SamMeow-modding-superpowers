using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using LeaveItThere.Components;
using SPT.Reflection.Patching;

namespace LeaveItThere.Patches;

internal class LootExperiencePatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(BaseStatisticsManager), "OnLootItem");
	}

	[PatchPrefix]
	public static bool Prefix()
	{
		if (!Singleton<GameWorld>.Instantiated)
		{
			return true;
		}
		return LITSession.Instance.LootExperienceEnabled;
	}
}

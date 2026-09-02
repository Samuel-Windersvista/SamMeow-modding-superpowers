using System;
using System.Reflection;
using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using LeaveItThere.Common;
using LeaveItThere.Components;
using LeaveItThere.Helpers;
using SPT.Reflection.Patching;

namespace LeaveItThere.Patches;

internal class GetAvailableActionsPatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(InteractionContextHelper), "GetAvailableActions", new Type[] { typeof(GamePlayerOwner), typeof(IInteractive) });
	}

	[PatchPrefix]
	private static bool PatchPrefix(GamePlayerOwner owner, object interactive, ref AvailableInteractionState __result)
	{
		if (!(interactive is FakeItem))
		{
			return true;
		}
		FakeItem fakeItem = interactive as FakeItem;
		AvailableInteractionState val = new AvailableInteractionState
		{
			Actions = CustomInteraction.GetActionsTypesClassList(fakeItem.Interactions)
		};
		__result = val;
		return false;
	}

	[PatchPostfix]
	private static void PatchPostfix(GamePlayerOwner owner, object interactive, ref AvailableInteractionState __result)
	{
		if (!(interactive is LootItem))
		{
			return;
		}
		LootItem val = (LootItem)((interactive is LootItem) ? interactive : null);
		if (LootItemIsTarget(val))
		{
			CustomInteraction customInteraction = new FakeItem.PlaceItemInteraction((val is ObservedLootItem) ? val : null);
			if (!ItemHelper.ItemCanBePickedUp(val.Item))
			{
				string name = ("No Space (" + val.Name.Localized((string)null) + ")").Localized((string)null);
				__result.Actions.Insert(0, new CustomInteraction.DisabledInteraction(name).GetActionsTypesClass());
			}
			__result.Actions.Add(customInteraction.GetActionsTypesClass());
		}
	}

	private static bool LootItemIsTarget(LootItem lootItem)
	{
		if (Plugin.PlaceableItemFilter.WhitelistEnabled && !Plugin.PlaceableItemFilter.WhitelistSet.Contains(lootItem.Item.StringTemplateId))
		{
			return false;
		}
		if (Plugin.PlaceableItemFilter.BlacklistEnabled && Plugin.PlaceableItemFilter.BlacklistSet.Contains(lootItem.Item.StringTemplateId))
		{
			return false;
		}
		if (lootItem is Corpse)
		{
			return false;
		}
		if (Settings.MinimumCostItemsArePlaceable.Value)
		{
			return true;
		}
		return ItemHelper.GetItemCost(lootItem.Item) > Settings.MinimumPlacementCost.Value;
	}
}

using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Console.Core;
using EFT.Interactive;
using EFT.UI;
using LeaveItThere.Components;
using LeaveItThere.Fika;
using LeaveItThere.Helpers;
using UnityEngine;

namespace LeaveItThere.Common;

public class ConsoleCommands
{
	[ConsoleCommand("lit_unplace_all_items_below_cost", "", null, "Un-Place all items on the map below a cost amount. If you run this command in error somehow, ALT F4 to avoid the changes being saved.", new string[] { })]
	public static void ClearPlacedItemsUnderCost([ConsoleArgument(0, "Cost Amount")] int costAmount, [ConsoleArgument("", "type 'IAMSURE' to confirm")] string iAmSure)
	{
		if (!(iAmSure != "IAMSURE"))
		{
			ItemHelper.ForAllItemsUnderCost(costAmount, delegate(FakeItem fakeItem)
			{
				FikaBridge.SendPlacedStateChangedPacket(fakeItem, isPlaced: false);
				fakeItem.Reclaim();
			});
		}
	}

	[ConsoleCommand("lit_teleport_all_placed_items_to_player", "", null, "Teleport items below cost to the player. If you run this command in error somehow, ALT F4 to avoid the changes being saved.", new string[] { })]
	public static void TPAllItemsUnderCostToPlayer([ConsoleArgument(0, "Cost Amount")] int costAmount, [ConsoleArgument("", "type 'IAMSURE' to confirm")] string iAmSure)
	{
		if (!(iAmSure != "IAMSURE"))
		{
			ItemHelper.ForAllItemsUnderCost(costAmount, delegate(FakeItem fakeItem)
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				LITSession instance = LITSession.Instance;
				fakeItem.PlaceAtPosition(LITUtils.PlayerFront, instance.Player.Transform.rotation);
				FikaBridge.SendPlacedStateChangedPacket(fakeItem, isPlaced: true);
			});
		}
	}

	[ConsoleCommand("lit_teleport_item_to_player", "", null, "Teleport item to the player. If you run this command in error somehow, ALT F4 to avoid the changes being saved.", new string[] { })]
	public static void TPItemToPlayer([ConsoleArgument(0, "Item number via lit_list_placed_items command")] int itemNum, [ConsoleArgument("", "type 'IAMSURE' to confirm")] string iAmSure)
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		if (!(iAmSure != "IAMSURE"))
		{
			LITSession instance = LITSession.Instance;
			if (itemNum > instance.FakeItems.Count - 1)
			{
				ConsoleScreen.LogError("No placed item found!");
				return;
			}
			FakeItem fakeItem = instance.FakeItems.Values.ToList()[itemNum];
			ConsoleScreen.Log("Teleporting item!");
			fakeItem.PlaceAtPosition(LITUtils.PlayerFront, instance.Player.Transform.rotation);
			FikaBridge.SendPlacedStateChangedPacket(fakeItem, isPlaced: true);
		}
	}

	[ConsoleCommand("lit_list_placed_items", "", null, "List information about all placed items on the map.", new string[] { })]
	public static void ListPlacedItems()
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		int num = 0;
		ConsoleScreen.Log("---------------------------------------");
		foreach (KeyValuePair<string, FakeItem> fakeItem in LITSession.Instance.FakeItems)
		{
			FakeItem value = fakeItem.Value;
			Vector3 position = Singleton<GameWorld>.Instance.MainPlayer.Transform.position;
			string text = string.Format("({0})".Localized((string)null), ((LootItem)value.LootItem).Name.Localized((string)null));
			string cardinalDirection = LITUtils.GetCardinalDirection(position, ((Component)value).gameObject.transform.position);
			string text2 = Vector3.Distance(position, ((Component)value).gameObject.transform.position).ToString();
			ConsoleScreen.Log($"{text} (item number: {num}) placed {text2} units away from player ({cardinalDirection})");
			num++;
		}
		ConsoleScreen.Log("---------------------------------------");
	}
}

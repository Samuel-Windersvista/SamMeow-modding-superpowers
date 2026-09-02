using System.Collections.Generic;
using EFT.Interactive;
using EFT.InventoryLogic;
using LeaveItThere.Components;
using LeaveItThere.Helpers;
using Newtonsoft.Json;
using UnityEngine;

namespace LeaveItThere.Common;

internal class PlacedItemData
{
	public Vector3 Location;

	public Quaternion Rotation;

	[JsonProperty("_itemDataBase64")]
	private string _itemDataBase64;

	[JsonIgnore]
	private Item _item;

	public Dictionary<string, object> AddonData = new Dictionary<string, object>();

	[JsonIgnore]
	public Item Item
	{
		get
		{
			if (_item == null)
			{
				_item = ItemHelper.StringToItem(_itemDataBase64);
			}
			return _item;
		}
	}

	public PlacedItemData()
	{
	}

	public PlacedItemData(FakeItem fakeItem)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		Location = ((Component)fakeItem).gameObject.transform.position;
		Rotation = ((Component)fakeItem).gameObject.transform.rotation;
		_itemDataBase64 = ItemHelper.ItemToString(((LootItem)fakeItem.LootItem).Item);
		AddonData = fakeItem.AddonData;
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using LeaveItThere.Components;
using UnityEngine;

namespace LeaveItThere.Helpers;

public static class ItemHelper
{
	private static FieldInfo _idFieldInfo;

	public static LootItem GetLootItem(string itemId)
	{
		if (LITSession.Instance.TryGetFakeItem(itemId, out var _))
		{
			LootItem spawnedLootItemFast = LITSession.Instance.GetSpawnedLootItemFast(itemId);
			if ((UnityEngine.Object)(object)spawnedLootItemFast != (UnityEngine.Object)null)
			{
				return spawnedLootItemFast;
			}
		}
		foreach (LootItem item in LITSession.Instance.GameWorld.LootItems.GetValuesEnumerator())
		{
			if (item.ItemId == itemId)
			{
				return item;
			}
		}
		return null;
	}

	public static void SpawnItem(Item item, Vector3 position, Quaternion rotation = default(Quaternion), Action<LootItem> callback = null, object genericCallbackArg = null, Action<object> genericCallback = null)
	{
		StaticManager.BeginCoroutine(SpawnItemRoutine(item, position, rotation, callback, genericCallbackArg, genericCallback));
	}

	public static void MoveItemToContainer(CompoundItem container, Item item)
	{
		Grid[] grids = container.Grids;
		foreach (Grid val in grids)
		{
			LocationInGrid val2 = val.FindFreeSpace(item);
			if (val2 != null)
			{
				val.Add(item, val2, false);
			}
		}
	}

	public static bool ContainerHasSpaceForItem(CompoundItem container, Item item)
	{
		Grid[] grids = container.Grids;
		for (int i = 0; i < grids.Length; i++)
		{
			if (grids[i].FindFreeSpace(item) != null)
			{
				return true;
			}
		}
		return false;
	}

	public static void SpawnItemInContainer(CompoundItem container, Item item, Action<LootItem> callback = null)
	{
		List<object> genericCallbackArg = new List<object>(2) { item, container };
		SpawnItem(item, default(Vector3), default(Quaternion), callback, genericCallbackArg, delegate(object arg)
		{
			List<object> obj = arg as List<object>;
			Item val = (Item)((obj[0] is Item) ? obj[0] : null);
			CompoundItem container2 = (obj[1] is CompoundItem) ? ((CompoundItem)obj[1]) : null;
			val.CurrentAddress = null;
			MoveItemToContainer(container2, val);
		});
	}

	public static bool RemoveItemFromContainer(CompoundItem container, Item itemToRemove)
	{
		Grid[] grids = container.Grids;
		foreach (Grid val in grids)
		{
			if (val.Items.Contains(itemToRemove))
			{
				return val.Remove(itemToRemove, false).Succeeded;
			}
		}
		return false;
	}

	public static IEnumerator SpawnItemRoutine(Item item, Vector3 position, Quaternion rotation = default(Quaternion), Action<LootItem> callback = null, object genericCallbackArg = null, Action<object> genericCallback = null)
	{
		if (!Singleton<GameWorld>.Instantiated)
		{
			throw new Exception("Tried to spawn an item while GameWorld was not instantiated!");
		}
		_ = Singleton<EFT.ItemFactory>.Instance;
		_ = Singleton<GameWorld>.Instance;
		List<ResourceKey> bundleResourceKeys = GetBundleResourceKeys(item);
		EFT.ObjectsFactory instance = Singleton<EFT.ObjectsFactory>.Instance;
		List<ResourceKey> list = bundleResourceKeys;
		List<ResourceKey> list2 = new List<ResourceKey>(list.Count);
		list2.AddRange(list);
		Task loadTask = instance.LoadBundlesAndCreatePools((EFT.ObjectsFactory.PoolsCategory)0, (EFT.ObjectsFactory.AssemblyType)1, list2, Diz.Jobs.JobYieldPriority.Immediate, null, default(CancellationToken));
		while (!loadTask.IsCompleted)
		{
			yield return (object)new WaitForEndOfFrame();
		}
		if (loadTask.IsFaulted)
		{
			Plugin.LogSource.LogError((object)$"Failed to load bundles for item {item.ShortName}: {loadTask.Exception}");
			yield break;
		}
		LootItem obj = SetupItem(item, new Vector3(-99999f, -99999f, -99999f), Quaternion.identity);
		genericCallback?.Invoke(genericCallbackArg);
		callback?.Invoke(obj);
	}

	private static List<ResourceKey> GetBundleResourceKeys(Item item)
	{
		List<ResourceKey> list = new List<ResourceKey>();
		foreach (Item allItem in item.GetAllItems())
		{
			foreach (ResourceKey allResource in allItem.Template.AllResources)
			{
				list.Add(allResource);
			}
		}
		return list;
	}

	public static byte[] ItemToBytes(Item item)
	{
		Mirror.NetworkWriter val = new Mirror.NetworkWriter();
		EFT.ItemDescriptor val2 = EFT.ItemBinarySerializer.SerializeItem(item, (ISearchController)Singleton<GameWorld>.Instance.MainPlayer.SearchController);
		EFT.BinarySerialization.BinarySerializationMirrorExtensions.WriteEFTItemDescriptor(val, val2);
		return val.ToArray();
	}

	public static string ItemToString(Item item)
	{
		return Convert.ToBase64String(ItemToBytes(item));
	}

	public static Item BytesToItem(byte[] bytes)
	{
		try
		{
			return EFT.ItemBinarySerializer.DeserializeItem(EFT.BinarySerialization.BinarySerializationMirrorExtensions.ReadEFTItemDescriptor(new Mirror.NetworkReader(new ArraySegment<byte>(bytes))), Singleton<EFT.ItemFactory>.Instance, new Dictionary<MongoID, Item>());
		}
		catch (Exception ex)
		{
			string text = "Failed to deserialize item from LeaveItThere-ItemData!";
			string text2 = "This is usually caused by placing a modded item, updating the mod to a new version with changes made to that item,";
			string text3 = "then trying to load into the map where it was placed. The item (and any contents if any) will be lost.";
			string text4 = "Alt F4 and downgrade to an older version of culprit mod and unplace items before re-updating.";
			string text5 = "Auto backups can also be used if needed, located in user/profiles/LeaveItThere-ItemData/[your_profile_id]/backups";
			string text6 = text + text2 + text3 + text4 + text5;
			Plugin.LogSource.LogWarning((object)text6);
			ConsoleScreen.LogWarning(text5);
			ConsoleScreen.LogWarning(text4);
			ConsoleScreen.LogWarning(text3);
			ConsoleScreen.LogWarning(text2);
			ConsoleScreen.LogWarning(text);
			InteractionHelper.NotificationLongWarning("Problem spawning item! Press ~ for more info!");
			Singleton<GUISounds>.Instance.PlayUISound((EUISoundType)6);
			Plugin.LogSource.LogError((object)ex);
			return null;
		}
	}

	public static Item StringToItem(string base64String)
	{
		return BytesToItem(Convert.FromBase64String(base64String));
	}

	public static void MakeSearchableItemFullySearched(SearchableItem searchableItem)
	{
		IPlayerSearchController controller = Singleton<GameWorld>.Instance.MainPlayer.SearchController;
		controller.SetItemAsSearched<SearchableItem>(searchableItem);
		ForAllChildrenInItem(searchableItem, delegate(Item item)
		{
			controller.SetItemAsKnown(item, false);
			if (item is SearchableItem)
			{
				controller.SetItemAsSearched<SearchableItem>((SearchableItem)item);
			}
		});
	}

	public static void ForAllChildrenInItem(Item parent, Action<Item> callable)
	{
		if (!(parent is CompoundItem))
		{
			return;
		}
		CompoundItem val = (CompoundItem)parent;
		Grid[] grids = val.Grids;
		for (int i = 0; i < grids.Length; i++)
		{
			foreach (Item item in grids[i].Items)
			{
				callable(item);
				ForAllChildrenInItem(item, callable);
			}
		}
		Slot[] slots = val.Slots;
		for (int i = 0; i < slots.Length; i++)
		{
			foreach (Item item2 in slots[i].Items)
			{
				callable(item2);
				ForAllChildrenInItem(item2, callable);
			}
		}
	}

	public static object[] RemoveLostInsuredItemsByIds(object[] lostInsuredItems, List<string> idsToRemove)
	{
		if (_idFieldInfo == null && lostInsuredItems.Any())
		{
			_idFieldInfo = lostInsuredItems[0].GetType().GetField("_id");
		}
		List<object> list = new List<object>();
		foreach (object obj in lostInsuredItems)
		{
			string item = _idFieldInfo.GetValue(obj).ToString();
			if (!idsToRemove.Contains(item))
			{
				list.Add(obj);
			}
		}
		return list.ToArray();
	}

	public static void SetItemColor(Color color, GameObject gameObject)
	{
		MeshRenderer[] componentsInChildren = gameObject.GetComponentsInChildren<MeshRenderer>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			Renderer val = componentsInChildren[i];
			if (val.material.HasProperty("_Color"))
			{
				val.material.color = color;
			}
		}
	}

	public static int GetItemCost(Item item, bool ignoreMinimumCostSetting = false)
	{
		if (LITSession.CostOverrides.ContainsKey(item.StringTemplateId))
		{
			return LITSession.CostOverrides[item.StringTemplateId];
		}
		int num = 0;
		if (item is SearchableItem)
		{
			Grid[] grids = ((CompoundItem)item).Grids;
			foreach (Grid val in grids)
			{
				num += val.GridWidth * val.GridHeight;
			}
		}
		else
		{
			IntVec2 val2 = item.CalculateCellSize();
			num += val2.X * val2.Y;
		}
		if (ignoreMinimumCostSetting)
		{
			return num;
		}
		if (num < Settings.MinimumPlacementCost.Value)
		{
			return Settings.MinimumPlacementCost.Value;
		}
		return num;
	}

	public static void ForAllItemsUnderCost(int costAmount, Action<FakeItem> callable)
	{
		foreach (KeyValuePair<string, FakeItem> fakeItem in LITSession.Instance.FakeItems)
		{
			FakeItem value = fakeItem.Value;
			if (GetItemCost(((LootItem)value.LootItem).Item, ignoreMinimumCostSetting: true) <= costAmount)
			{
				callable(value);
			}
		}
	}

	public static bool ItemCanBePickedUp(Item item)
	{
		InventoryController inventoryController = LITSession.Instance.Player.InventoryController;
		InventoryEquipment equipment = inventoryController.Inventory.Equipment;
		return ItemManipulator.QuickFindAppropriatePlace(item, inventoryController, new CompoundItem[] { equipment }, ItemManipulator.EMoveItemOrder.PickUp, true).Succeeded;
	}

	public static LootItem SetupItem(Item item, Vector3 position, Quaternion rotation)
	{
		GameObject val = Singleton<EFT.ObjectsFactory>.Instance.CreateLootPrefab(item, Player.GetVisibleToCamera(LITSession.Instance.Player), null);
		val.SetActive(true);
		BoxCollider val2 = default(BoxCollider);
		LootItem obj = LITSession.Instance.GameWorld.CreateLootWithRigidbody(val, item, item.ShortName, false, null, out val2, true, true, 0f);
		Rigidbody component = ((Component)obj).GetComponent<Rigidbody>();
		component.collisionDetectionMode = (CollisionDetectionMode)3;
		component.isKinematic = true;
		((Component)obj).transform.SetPositionAndRotation(position, rotation);
		return obj;
	}
}

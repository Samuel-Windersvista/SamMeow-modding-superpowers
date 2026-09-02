using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LeaveItThere.Addon;
using LeaveItThere.Common;
using LeaveItThere.Helpers;
using Newtonsoft.Json;
using UnityEngine;

namespace LeaveItThere.Components;

public class LITSession : MonoBehaviour
{
	public static Dictionary<string, int> CostOverrides = new Dictionary<string, int>();

	public Dictionary<string, FakeItem> FakeItems = new Dictionary<string, FakeItem>();

	private Dictionary<string, LootItem> _spawnedLootItemLookup = new Dictionary<string, LootItem>();

	private static LITSession _instance = null;

	private int _pointsSpent;

	private static int _itemsSpawned;

	private static int _itemsToSpawn;

	public bool InteractionsAllowed { get; private set; } = true;

	public bool LootExperienceEnabled { get; private set; } = true;

	public GameWorld GameWorld { get; private set; }

	public Player Player { get; private set; }

	public GamePlayerOwner GamePlayerOwner { get; private set; }

	public static LITSession Instance
	{
		get
		{
			if (!Singleton<GameWorld>.Instantiated)
			{
				throw new Exception("Tried to get ModSession when game world was not instantiated!");
			}
			if ((UnityEngine.Object)(object)_instance == (UnityEngine.Object)null)
			{
				_instance = GameObjectExtensions.GetOrAddComponent<LITSession>(Singleton<GameWorld>.Instance.MainPlayer.gameObject);
			}
			return _instance;
		}
	}

	public int PointsSpent
	{
		get
		{
			return _pointsSpent;
		}
		private set
		{
			_pointsSpent = Mathf.Clamp(value, 0, Settings.GetAllottedPoints());
		}
	}

	public Dictionary<string, object> GlobalAddonData { get; private set; } = new Dictionary<string, object>();

	private LITSession()
	{
	}

	private void Awake()
	{
		GameWorld = Singleton<GameWorld>.Instance;
		Player = GameWorld.MainPlayer;
		GamePlayerOwner = ((Component)Player).GetComponent<GamePlayerOwner>();
		SpawnAllPlacedItems();
	}

	internal static void CreateNewModSession()
	{
		_instance = GameObjectExtensions.GetOrAddComponent<LITSession>(Singleton<GameWorld>.Instance.MainPlayer.gameObject);
	}

	private void SpawnAllPlacedItems()
	{
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		PlacedItemDataPack placedItemDataPack = LITUtils.ServerRoute("/jehree/pip/data_to_client", PlacedItemDataPack.Request);
		GlobalAddonData = placedItemDataPack.GlobalAddonData;
		_itemsSpawned = 0;
		_itemsToSpawn = placedItemDataPack.ItemTemplates.Count;
		if (_itemsToSpawn > 0)
		{
			LootExperienceEnabled = false;
		}
		for (int i = 0; i < placedItemDataPack.ItemTemplates.Count; i++)
		{
			PlacedItemData data = placedItemDataPack.ItemTemplates[i];
			if (data.Item == null)
			{
				_itemsSpawned++;
				if (_itemsSpawned >= _itemsToSpawn)
				{
					Instance.LootExperienceEnabled = true;
					LITStaticEvents.InvokeOnLastPlacedItemSpawned(null);
				}
				continue;
			}
			ItemHelper.SpawnItem(data.Item, new Vector3(0f, -9999f, 0f), data.Rotation, delegate(LootItem lootItem)
			{
				//IL_004c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0057: Unknown result type (might be due to invalid IL or missing references)
				if (lootItem.Item is SearchableItem)
				{
					Item item = lootItem.Item;
					ItemHelper.MakeSearchableItemFullySearched((SearchableItem)item);
				}
				Instance.RegisterSpawnedLootItem(lootItem.ItemId, lootItem);
				FakeItem fakeItem = FakeItem.CreateNewFakeItem((ObservedLootItem)(object)((lootItem is ObservedLootItem) ? lootItem : null), data.AddonData);
				fakeItem.PlaceAtPosition(data.Location, data.Rotation);
				LITStaticEvents.InvokeOnPlacedItemSpawned(fakeItem);
				fakeItem.InvokeOnFakeItemSpawned();
				_itemsSpawned++;
				if (_itemsSpawned >= _itemsToSpawn)
				{
					Instance.LootExperienceEnabled = true;
					LITStaticEvents.InvokeOnLastPlacedItemSpawned(fakeItem);
				}
			});
		}
	}

	internal void RegisterSpawnedLootItem(string itemId, LootItem lootItem)
	{
		_spawnedLootItemLookup[itemId] = lootItem;
	}

	internal void UnregisterSpawnedLootItem(string itemId)
	{
		_spawnedLootItemLookup.Remove(itemId);
	}

	public LootItem GetSpawnedLootItemFast(string itemId)
	{
		_spawnedLootItemLookup.TryGetValue(itemId, out var value);
		return value;
	}

	internal void AddFakeItem(FakeItem fakeItem)
	{
		if (!FakeItems.ContainsKey(fakeItem.ItemId))
		{
			FakeItems[fakeItem.ItemId] = fakeItem;
		}
	}

	internal void RemoveFakeItem(FakeItem fakeItem)
	{
		if (FakeItems.ContainsKey(fakeItem.ItemId))
		{
			FakeItems.Remove(fakeItem.ItemId);
			UnregisterSpawnedLootItem(fakeItem.ItemId);
		}
	}

	public FakeItem GetFakeItemOrNull(string itemId)
	{
		if (!FakeItems.ContainsKey(itemId))
		{
			return null;
		}
		return FakeItems[itemId];
	}

	public bool TryGetFakeItem(string itemId, out FakeItem fakeItem)
	{
		fakeItem = GetFakeItemOrNull(itemId);
		return (UnityEngine.Object)(object)fakeItem != (UnityEngine.Object)null;
	}

	public bool PlacementIsAllowed(Item item)
	{
		if (Settings.CostSystemEnabled.Value)
		{
			return PointsSpent + ItemHelper.GetItemCost(item) <= Settings.GetAllottedPoints();
		}
		return true;
	}

	internal void SpendPoints(int points)
	{
		PointsSpent += points;
	}

	internal void RefundPoints(int points)
	{
		PointsSpent -= points;
	}

	public List<string> GetPlacedItemInstanceIds()
	{
		List<string> ids = new List<string>();
		foreach (KeyValuePair<string, FakeItem> fakeItem in FakeItems)
		{
			if (!((UnityEngine.Object)(object)fakeItem.Value == (UnityEngine.Object)null) && !((UnityEngine.Object)(object)fakeItem.Value.LootItem == (UnityEngine.Object)null))
			{
				ids.Add(((LootItem)fakeItem.Value.LootItem).Item.Id);
				ItemHelper.ForAllChildrenInItem(((LootItem)fakeItem.Value.LootItem).Item, delegate(Item item)
				{
					ids.Add(item.Id);
				});
			}
		}
		return ids;
	}

	internal void DestroyAllFakeItems()
	{
		foreach (KeyValuePair<string, FakeItem> fakeItem in FakeItems)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)fakeItem.Value).gameObject);
		}
		_instance = null;
	}

	internal void SendPlacedItemDataToServer()
	{
		PlacedItemDataPack data = new PlacedItemDataPack(GlobalAddonData, GetPlacedItemDataListToSave());
		LITUtils.ServerRouteAsync("/jehree/pip/data_to_server", data);
	}

	private List<PlacedItemData> GetPlacedItemDataListToSave()
	{
		List<PlacedItemData> list = new List<PlacedItemData>();
		foreach (KeyValuePair<string, FakeItem> fakeItem in Instance.FakeItems)
		{
			FakeItem value = fakeItem.Value;
			list.Add(new PlacedItemData(value));
		}
		return list;
	}

	public void SetInteractionsEnabled(bool enabled)
	{
		InteractionsAllowed = enabled;
	}

	public T GetGlobalAddonDataOrNull<T>(string key) where T : class
	{
		if (!GlobalAddonData.ContainsKey(key))
		{
			return null;
		}
		if (!(GlobalAddonData[key] is T))
		{
			T value = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(GlobalAddonData[key]));
			GlobalAddonData[key] = value;
		}
		return (T)GlobalAddonData[key];
	}

	public void PutGlobalAddonData<T>(string key, T data) where T : class
	{
		GlobalAddonData[key] = data;
	}
}

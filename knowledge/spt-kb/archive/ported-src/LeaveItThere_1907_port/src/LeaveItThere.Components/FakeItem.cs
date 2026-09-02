using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using LeaveItThere.Addon;
using LeaveItThere.Common;
using LeaveItThere.Fika;
using LeaveItThere.Helpers;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.AI;

namespace LeaveItThere.Components;

public class FakeItem : InteractableObject
{
	public class AddonFlags
	{
		public bool MoveModeDisabled;

		public string MoveModeDisabledReason = "Disabled";

		public bool ReclaimInteractionDisabled;

		public string ReclaimInteractionDisabledReason = "Disabled";

		public bool IsPhysicalRegardlessOfSize;

		public bool RemoveRootCollider;
	}

	public delegate void SpawnedHandler();

	public delegate void PlacedStateChangedHandler(bool isPlaced);

	public class SearchInteraction : CustomInteraction
	{
		private EFT.InventoryLogic.SearchableItem _searchableItem;

		public override string Name => "Search";

		public SearchInteraction(FakeItem fakeItem)
			: base(fakeItem)
		{
			_searchableItem = (EFT.InventoryLogic.SearchableItem)((LootItem)base.FakeItem.LootItem).Item;
		}

		public override void OnInteract()
		{
			Singleton<GameWorld>.Instance.MainPlayer.SearchController.SearchContents(_searchableItem);
		}
	}

	public class PlaceItemInteraction(LootItem lootItem) : CustomInteraction
	{
		private LootItem _lootItem = lootItem;

		public override string Name => "Place Item";

		public override bool Enabled => LITSession.Instance.PlacementIsAllowed(_lootItem.Item);

		public override void OnInteract()
		{
			LootItem lootItem = _lootItem;
			FakeItem fakeItem = CreateNewFakeItem((ObservedLootItem)(object)((lootItem is ObservedLootItem) ? lootItem : null));
			fakeItem.PlaceAtLootItem();
			fakeItem.PlacedPlayerFeedback();
			FikaBridge.SendPlacedStateChangedPacket(fakeItem, isPlaced: true);
		}
	}

	public class ReclaimInteraction : CustomInteraction
	{
		public override string Name
		{
			get
			{
				if (!base.FakeItem.Flags.ReclaimInteractionDisabled)
				{
					return "Reclaim";
				}
				return "Reclaim: " + base.FakeItem.Flags.ReclaimInteractionDisabledReason;
			}
		}

		public override string TargetName => ((LootItem)base.FakeItem.LootItem).Name.Localized((string)null);

		public override bool Enabled => !base.FakeItem.Flags.ReclaimInteractionDisabled;

		public override bool AutoPromptRefresh => true;

		public ReclaimInteraction(FakeItem fakeItem)
			: base(fakeItem)
		{
		}

		public override void OnInteract()
		{
			FikaBridge.SendPlacedStateChangedPacket(base.FakeItem, isPlaced: false);
			base.FakeItem.Reclaim();
			base.FakeItem.ReclaimPlayerFeedback();
		}
	}

	private NavMeshObstacle _obstacle;

	private ObservedLootItem _lootItem;

	public List<CustomInteraction> Interactions = new List<CustomInteraction>();

	public AddonFlags Flags { get; private set; } = new AddonFlags();

	public Dictionary<string, object> AddonData { get; private set; } = new Dictionary<string, object>();

	public ObservedLootItem LootItem
	{
		get
		{
			if ((UnityEngine.Object)(object)_lootItem == (UnityEngine.Object)null || ((LootItem)_lootItem).Item == null || ((LootItem)_lootItem).Item.Id == null)
			{
				LootItem lootItem = ItemHelper.GetLootItem(ItemId);
				ObservedLootItem val = (ObservedLootItem)(object)((lootItem is ObservedLootItem) ? lootItem : null);
				if (val != null)
				{
					_lootItem = val;
				}
				else if ((UnityEngine.Object)(object)lootItem != (UnityEngine.Object)null)
				{
					Plugin.LogSource.LogError((object)("LootItem for FakeItem " + ItemId + " is not ObservedLootItem"));
				}
			}
			return _lootItem;
		}
		private set
		{
			_lootItem = value;
		}
	}

	public string ItemId { get; private set; }

	public string TemplateId { get; private set; }

	public event SpawnedHandler OnSpawned;

	public event PlacedStateChangedHandler OnPlacedStateChanged;

	internal void InvokeOnFakeItemSpawned()
	{
		this.OnSpawned?.Invoke();
	}

	private void Init(ObservedLootItem lootItem)
	{
		LootItem = lootItem;
		ItemId = ((LootItem)lootItem).ItemId;
		TemplateId = ((LootItem)lootItem).TemplateId;
		AddNavMeshObstacle();
		LITSession.Instance.AddFakeItem(this);
		if (LootItem.Item is EFT.InventoryLogic.SearchableItem)
		{
			Interactions.Add(new SearchInteraction(this));
		}
		Interactions.Add(new ItemMover.EnterMoveModeInteraction(this));
		LITStaticEvents.InvokeOnFakeItemInitialized(this);
		SetPlayerAndBotCollisionEnabled(Settings.PlacedItemsHaveCollision.Value);
		Interactions.Add(new ReclaimInteraction(this));
		if (Flags.RemoveRootCollider || ((UnityEngine.Object)((Component)this).gameObject).name.Contains("LITRemoveRootCollider"))
		{
			MyExtensions.ExecuteForEach<BoxCollider>(((Component)this).GetComponents<BoxCollider>(), delegate(BoxCollider col)
			{
				((Collider)col).enabled = false;
			});
		}
	}

	internal static FakeItem CreateNewFakeItem(ObservedLootItem lootItem, Dictionary<string, object> addonData = null)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		GameObject val = UnityEngine.Object.Instantiate<GameObject>(((Component)lootItem).gameObject);
		ItemHelper.SetItemColor(Settings.PlacedItemTint.Value, val.gameObject);
		val.transform.position = ((Component)lootItem).gameObject.transform.position;
		val.transform.rotation = ((Component)lootItem).gameObject.transform.rotation;
		val.transform.localScale = val.transform.localScale * 0.99f;
		ObservedLootItem component = val.GetComponent<ObservedLootItem>();
		if ((UnityEngine.Object)(object)component != (UnityEngine.Object)null)
		{
			UnityEngine.Object.Destroy((UnityEngine.Object)(object)component);
		}
		FakeItem fakeItem = val.AddComponent<FakeItem>();
		if (addonData != null)
		{
			fakeItem.AddonData = addonData;
		}
		fakeItem.Init(lootItem);
		return fakeItem;
	}

	private void AddNavMeshObstacle()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		_obstacle = GameObjectExtensions.GetOrAddComponent<NavMeshObstacle>(((Component)this).gameObject);
		BoxCollider component = ((Component)this).gameObject.GetComponent<BoxCollider>();
		_obstacle.shape = (NavMeshObstacleShape)1;
		_obstacle.center = component.center;
		_obstacle.size = component.size;
		_obstacle.carving = true;
		_obstacle.carveOnlyStationary = true;
	}

	public void SetPlayerAndBotCollisionEnabled(bool enabled)
	{
		if (enabled)
		{
			bool flag = ((LootItem)LootItem).Item.Width * ((LootItem)LootItem).Item.Height < Settings.MinimumSizeItemToGetCollision.Value;
			if (!Flags.IsPhysicalRegardlessOfSize && flag)
			{
				return;
			}
		}
		((Behaviour)_obstacle).enabled = enabled;
		LITUtils.ForAllDescendants(((Component)this).gameObject, delegate(GameObject descendant)
		{
			if (!((UnityEngine.Object)(object)descendant.GetComponent<Collider>() == (UnityEngine.Object)null) && !((UnityEngine.Object)descendant).name.Contains("LITKeepLayer"))
			{
				if (enabled)
				{
					descendant.layer = GetCollisionEnabledLayerNumber(((UnityEngine.Object)descendant).name);
				}
				else
				{
					descendant.layer = LayerMask.NameToLayer("Loot");
				}
			}
		});
	}

	public static int GetCollisionEnabledLayerNumber(string objectName)
	{
		if (!objectName.Contains("LITSetLayer"))
		{
			return LayerMask.NameToLayer("Default");
		}
		int startIndex = objectName.IndexOf("LITSetLayer") + "LITSetLayer".Length;
		return int.Parse(objectName.Substring(startIndex, 2));
	}

	public void Reclaim()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		((Component)LootItem).gameObject.transform.position = ((Component)this).gameObject.transform.position;
		((Component)LootItem).gameObject.transform.rotation = ((Component)this).gameObject.transform.rotation;
		LITSession.Instance.RefundPoints(ItemHelper.GetItemCost(((LootItem)LootItem).Item));
		LITSession.Instance.RemoveFakeItem(this);
		LITStaticEvents.InvokeOnItemPlacedStateChanged(this, isPlaced: false);
		this.OnPlacedStateChanged?.Invoke(isPlaced: false);
		UnityEngine.Object.Destroy((UnityEngine.Object)(object)((Component)this).gameObject);
	}

	public void ReclaimPlayerFeedback()
	{
		LITSession instance = LITSession.Instance;
		if (Settings.CostSystemEnabled.Value)
		{
			InteractionHelper.NotificationLong($"Points rufunded: {ItemHelper.GetItemCost(((LootItem)LootItem).Item)}, {Settings.GetAllottedPoints() - instance.PointsSpent} out of {Settings.GetAllottedPoints()} points remaining");
		}
		Singleton<GUISounds>.Instance.PlayUISound((EUISoundType)34);
	}

	public void PlaceAtLootItem()
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		((LootItem)LootItem).StopPhysics();
		LITSession.Instance.SpendPoints(ItemHelper.GetItemCost(((LootItem)LootItem).Item));
		SetLocation(((Component)LootItem).gameObject.transform.position, ((Component)LootItem).gameObject.transform.rotation);
		((Component)LootItem).gameObject.transform.position = new Vector3(0f, -99999f, 0f);
		LITStaticEvents.InvokeOnItemPlacedStateChanged(this, isPlaced: true);
		this.OnPlacedStateChanged?.Invoke(isPlaced: true);
	}

	public void PlaceAtPosition(Vector3 position, Quaternion rotation)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		PlaceAtLootItem();
		SetLocation(position, rotation);
	}

	public void PlacedPlayerFeedback()
	{
		LITSession instance = LITSession.Instance;
		if (Settings.CostSystemEnabled.Value)
		{
			InteractionHelper.NotificationLong($"Placement cost: {ItemHelper.GetItemCost(((LootItem)LootItem).Item)}, {Settings.GetAllottedPoints() - instance.PointsSpent} out of {Settings.GetAllottedPoints()} points remaining");
		}
		Singleton<GUISounds>.Instance.PlayUISound((EUISoundType)33);
	}

	private void SetLocation(Vector3 position, Quaternion rotation)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		((Component)this).gameObject.transform.position = position;
		((Component)this).gameObject.transform.rotation = rotation;
	}

	public static void ErrorPlayerFeedback(string message)
	{
		InteractionHelper.NotificationLongWarning(message);
		Singleton<GUISounds>.Instance.PlayUISound((EUISoundType)6);
	}

	public T GetAddonDataOrNull<T>(string key) where T : class
	{
		if (!AddonData.ContainsKey(key))
		{
			return null;
		}
		if (!(AddonData[key] is T))
		{
			T value = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(AddonData[key]));
			AddonData[key] = value;
		}
		return (T)AddonData[key];
	}

	public void PutAddonData<T>(string key, T data) where T : class
	{
		AddonData[key] = data;
	}
}

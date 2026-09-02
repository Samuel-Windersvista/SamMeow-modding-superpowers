using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.Interactive;
using EFT.UI;
using HomeComforts.Components;
using HomeComforts.Helpers;
using LeaveItThere.Addon;
using LeaveItThere.Common;
using LeaveItThere.Components;
using UnityEngine;

namespace HomeComforts.Items.SpaceHeater;

internal class SpaceHeater : MonoBehaviour, IPhysicsTrigger
{
	public class ToggleSpaceHeaterInteraction : CustomInteraction
	{
		private readonly SpaceHeater _heater;

		public ToggleSpaceHeaterInteraction(FakeItem fakeItem, SpaceHeater heater)
			: base(fakeItem)
		{
			_heater = heater;
		}

		public SpaceHeater Heater => _heater;

		public override string Name => Heater.AOEEnabled ? "Turn Off" : "Turn On";

		public override bool AutoPromptRefresh => true;

		public override void OnInteract()
		{
			Heater.AOEEnabled = !Heater.AOEEnabled;
			if (Heater.AOEEnabled)
			{
				Singleton<GUISounds>.Instance.PlayUISound((EUISoundType)37);
			}
			else
			{
				Singleton<GUISounds>.Instance.PlayUISound((EUISoundType)38);
			}
			SpaceHeaterStatePacket.Instance.Send(FakeItem.ItemId, Heater.AOEEnabled);
			Heater.FakeItem.PutAddonData("Jehree.HomeComforts", SpaceHeaterAddonData.CreateData(Heater.AOEEnabled));
			HomeComfortsUtils.ForceUpdatePlayerCollisions();
		}
	}

	public class SpaceHeaterStatePacket : LITPacketRegistration
	{
		private class Data
		{
			public bool Enabled;

			public string HeaterId;
		}

		public static SpaceHeaterStatePacket Instance => LITPacketRegistration.Get<SpaceHeaterStatePacket>();

		public override void OnPacketReceived(Packet packet)
		{
			Data data = packet.GetData<Data>();
			SpaceHeater spaceHeater = HCSession.Instance.SpaceHeaterSession.GetSpaceHeaterOrNull(data.HeaterId);
			if (spaceHeater == null)
			{
				return;
			}
			spaceHeater.AOEEnabled = data.Enabled;
			spaceHeater.FakeItem.PutAddonData("Jehree.HomeComforts", SpaceHeaterAddonData.CreateData(data.Enabled));
		}

		public void Send(string heaterId, bool enabled)
		{
			SendData(new Data
			{
				Enabled = enabled,
				HeaterId = heaterId
			});
		}
	}

	public FakeItem FakeItem;

	private Collider _collider;

	public string Description { get; } = "Space Heater";

	public new bool enabled
	{
		get
		{
			return _collider.enabled;
		}
		set
		{
			_collider.enabled = value;
		}
	}

	public bool AOEEnabled
	{
		get
		{
			return _collider.enabled;
		}
		set
		{
			_collider.enabled = value;
		}
	}

	public static void OnFakeItemInitialized(FakeItem fakeItem)
	{
		if (Plugin.ServerConfig.SpaceHeaterItemIds.Contains(((LootItem)fakeItem.LootItem).Item.StringTemplateId))
		{
			SpaceHeater spaceHeater = AddSpaceHeaterBehavior(fakeItem);
			HCSession.Instance.SpaceHeaterSession.SpaceHeaters.Add(spaceHeater);
			fakeItem.OnPlacedStateChanged += spaceHeater.OnItemPlacedStateChanged;
			fakeItem.Interactions.Add(new ToggleSpaceHeaterInteraction(fakeItem, spaceHeater));
			SpaceHeaterAddonData addonData = fakeItem.GetAddonDataOrNull<SpaceHeaterAddonData>("Jehree.HomeComforts");
			if (addonData != null && addonData.HeaterEnabled)
			{
				spaceHeater.AOEEnabled = true;
			}
		}
	}

	private void OnItemPlacedStateChanged(bool isPlaced)
	{
		if (!isPlaced)
		{
			HCSession.Instance.SpaceHeaterSession.SpaceHeaters.Remove(this);
			AOEEnabled = false;
		}
	}

	public void OnTriggerEnter(Collider collider)
	{
		if (HCSession.Instance.GameWorld.GetPlayerByCollider(collider) == HCSession.Instance.Player)
		{
			if (!HCSession.Instance.SpaceHeaterSession.PlayerIsInSpaceHeaterZone)
			{
				HCSession.Instance.NeedsRateReductions.SetEnabled(enabled: true);
				NotificationManager.DisplayMessageNotification("Comfort Buff Active!", ENotificationDurationType.Long, ENotificationIconType.Default, null);
			}
			HCSession.Instance.SpaceHeaterSession.AddSpaceHeaterIdToPlayerIsIn(FakeItem.ItemId);
		}
	}

	public void OnTriggerExit(Collider collider)
	{
		if (HCSession.Instance.GameWorld.GetPlayerByCollider(collider) == HCSession.Instance.Player)
		{
			HCSession.Instance.SpaceHeaterSession.RemoveSpaceHeaterIdFromPlayerIsIn(FakeItem.ItemId);
			if (!HCSession.Instance.SpaceHeaterSession.PlayerIsInSpaceHeaterZone)
			{
				HCSession.Instance.NeedsRateReductions.SetEnabled(enabled: false);
				NotificationManager.DisplayWarningNotification("Comfort Buff Removed.", ENotificationDurationType.Long);
			}
		}
	}

	public static SpaceHeater AddSpaceHeaterBehavior(FakeItem fakeItem)
	{
		GameObject gameObject = GameObject.CreatePrimitive((PrimitiveType)0);
		gameObject.name = fakeItem.name + "_spaceheater_LITKeepLayer";
		gameObject.layer = LayerMask.NameToLayer("Triggers");
		gameObject.transform.localScale = Vector3.one * Settings.SpaceHeaterAOESizeMultiplier.Value;
		gameObject.GetComponent<Renderer>().enabled = false;
		SphereCollider component = gameObject.GetComponent<SphereCollider>();
		component.isTrigger = true;
		SpaceHeater spaceHeater = gameObject.AddComponent<SpaceHeater>();
		spaceHeater.Init(fakeItem, component);
		return spaceHeater;
	}

	private void Init(FakeItem fakeItem, SphereCollider collider)
	{
		FakeItem = fakeItem;
		gameObject.transform.SetParent(fakeItem.gameObject.transform);
		gameObject.transform.position = fakeItem.gameObject.transform.position;
		_collider = collider;
		AOEEnabled = false;
	}
}

using System.Collections.Generic;
using EFT;
using EFT.Communications;
using EFT.Interactive;
using HomeComforts.Components;
using HomeComforts.Helpers;
using LeaveItThere.Addon;
using LeaveItThere.Common;
using LeaveItThere.Components;
using Newtonsoft.Json;
using UnityEngine;

namespace HomeComforts.Items.Safehouse;

internal class SafehouseAddonData
{
	[JsonProperty("_profilesWithSafehouseEnabled")]
	private List<string> _profilesWithSafehouseEnabled = new List<string>();

	public void AddProfileId(string profileId)
	{
		if (!_profilesWithSafehouseEnabled.Contains(profileId))
		{
			_profilesWithSafehouseEnabled.Add(profileId);
		}
	}

	public void AddProfileId()
	{
		AddProfileId(HCSession.Instance.Player.ProfileId);
	}

	public void RemoveProfileId(string profileId)
	{
		_profilesWithSafehouseEnabled.Remove(profileId);
	}

	public void RemoveProfileId()
	{
		RemoveProfileId(HCSession.Instance.Player.ProfileId);
	}

	public bool ContainsMainPlayer()
	{
		return _profilesWithSafehouseEnabled.Contains(HCSession.Instance.Player.ProfileId);
	}
}

internal class SafehouseGlobalAddonData
{
	internal class ProfileData
	{
		public string ProfileId;

		public Vector3 InfilPosition;

		public string SafehouseId;
	}

	[JsonProperty("_profileDataLookup")]
	private Dictionary<string, ProfileData> _profileDataLookup = new Dictionary<string, ProfileData>();

	public void AddProfile(string profileId, Vector3 infilPos, string safehouseId)
	{
		_profileDataLookup[profileId] = new ProfileData
		{
			ProfileId = profileId,
			InfilPosition = infilPos,
			SafehouseId = safehouseId
		};
	}

	public void AddProfile()
	{
		string profileId = HCSession.Instance.Player.ProfileId;
		Vector3 position = HCSession.Instance.CustomSafehouseExfil.gameObject.transform.position;
		string itemId = ((LootItem)HCSession.Instance.CustomSafehouseExfil.LastSafehouseThatUsedMe.FakeItem.LootItem).ItemId;
		AddProfile(profileId, position, itemId);
	}

	public ProfileData GetProfile(string profileId)
	{
		if (!_profileDataLookup.ContainsKey(profileId))
		{
			return null;
		}
		return _profileDataLookup[profileId];
	}

	public ProfileData GetProfile()
	{
		return GetProfile(HCSession.Instance.Player.ProfileId);
	}

	public void RemoveProfile(string profileId)
	{
		_profileDataLookup.Remove(profileId);
	}

	public void RemoveProfile()
	{
		RemoveProfile(HCSession.Instance.Player.ProfileId);
	}

	public bool ContainsProfile(string profileId)
	{
		return _profileDataLookup.ContainsKey(profileId);
	}

	public bool ContainsProfile()
	{
		return ContainsProfile(HCSession.Instance.Player.ProfileId);
	}
}

internal class Safehouse : MonoBehaviour
{
	public class SafehouseEnabledStatePacket : LITPacketRegistration
	{
		private class Data
		{
			public string SafehouseId;

			public bool Enabled;
		}

		public static SafehouseEnabledStatePacket Instance => LITPacketRegistration.Get<SafehouseEnabledStatePacket>();

		public override EPacketDestination Destination => EPacketDestination.HostOnly;

		public override void OnPacketReceived(Packet packet)
		{
			Data data = packet.GetData<Data>();
			Safehouse safehouse = HCSession.Instance.SafehouseSession.GetSafehouseOrNull(data.SafehouseId);
			if (safehouse == null)
			{
				return;
			}
			if (data.Enabled)
			{
				safehouse.AddonData.AddProfileId(packet.SenderProfileId);
			}
			else
			{
				safehouse.AddonData.RemoveProfileId(packet.SenderProfileId);
			}
		}

		public void Send(string safehouseId, bool enabled)
		{
			SendData(new Data
			{
				SafehouseId = safehouseId,
				Enabled = enabled
			});
		}
	}

	public class ToggleSafehouseEnabledInteraction : CustomInteraction
	{
		private readonly Safehouse _safehouse;

		public ToggleSafehouseEnabledInteraction(FakeItem fakeItem, Safehouse safehouse)
			: base(fakeItem)
		{
			_safehouse = safehouse;
		}

		public Safehouse Safehouse => _safehouse;

		public override string Name => Safehouse.SafehouseEnabled ? "Disable Safehouse" : "Activate Safehouse";

		public override bool Enabled => Safehouse.SafehouseEnabled || HCSession.Instance.SafehouseSession.SafehouseEnableAllowed;

		public override bool AutoPromptRefresh => true;

		public override void OnInteract()
		{
			Safehouse.SetSafehouseEnabled(!Safehouse.SafehouseEnabled);
		}
	}

	public class ToggleExfilEnabledInteraction : CustomInteraction
	{
		private readonly Safehouse _safehouse;

		public ToggleExfilEnabledInteraction(FakeItem fakeItem, Safehouse safehouse)
			: base(fakeItem)
		{
			_safehouse = safehouse;
		}

		public Safehouse Safehouse => _safehouse;

		public SafehouseExfil Exfil => HCSession.Instance.CustomSafehouseExfil;

		public override string Name => Exfil.ExfilIsEnabled ? "Stop Extracting" : "Extract";

		public override bool Enabled => Safehouse.SafehouseEnabled;

		public override bool AutoPromptRefresh => true;

		public override void OnInteract()
		{
			Exfil.SetCustomExfilEnabled(!Exfil.ExfilIsEnabled);
			if (Exfil.ExfilIsEnabled)
			{
				Exfil.gameObject.transform.position = HCSession.Instance.Player.Transform.position;
				Exfil.LastSafehouseThatUsedMe = Safehouse;
			}
		}
	}

	public bool SafehouseEnabled;

	private SafehouseAddonData _addonData;

	public FakeItem FakeItem { get; private set; }

	public SafehouseAddonData AddonData
	{
		get
		{
			if (_addonData == null)
			{
				_addonData = FakeItem.GetAddonDataOrNull<SafehouseAddonData>("Jehree.HomeComforts");
			}
			if (_addonData == null)
			{
				SafehouseAddonData safehouseAddonData = new SafehouseAddonData();
				FakeItem.PutAddonData("Jehree.HomeComforts", safehouseAddonData);
				_addonData = safehouseAddonData;
			}
			return _addonData;
		}
	}

	private void OnPlacedStateChanged(bool isPlaced)
	{
		if (!isPlaced)
		{
			SetSafehouseEnabled(enabled: false);
			HCSession.Instance.SafehouseSession.RemoveSafehouse(this);
		}
	}

	private void OnPlacedItemSpawned()
	{
		if (AddonData.ContainsMainPlayer())
		{
			HCSession.Instance.InitialExfilPosition = transform.position;
			SetSafehouseEnabled(enabled: true);
		}
	}

	public static void OnFakeItemInitialized(FakeItem fakeItem)
	{
		if (Plugin.ServerConfig.SafehouseItemIds.Contains(((LootItem)fakeItem.LootItem).Item.StringTemplateId))
		{
			Transform transform = fakeItem.gameObject.transform;
			transform.localScale *= 2f;
			if (Settings.ScavsCanUseSafehouse.Value || (int)HCSession.Instance.Player.Side != 4)
			{
				Safehouse safehouse = fakeItem.gameObject.AddComponent<Safehouse>();
				safehouse.Init(fakeItem);
				fakeItem.Interactions.Add(new ToggleSafehouseEnabledInteraction(fakeItem, safehouse));
				fakeItem.Interactions.Add(new ToggleExfilEnabledInteraction(fakeItem, safehouse));
			}
		}
	}

	private void Init(FakeItem fakeItem)
	{
		FakeItem = fakeItem;
		HCSession.Instance.SafehouseSession.AddSafehouse(this);
		FakeItem.OnPlacedStateChanged += OnPlacedStateChanged;
		FakeItem.OnSpawned += OnPlacedItemSpawned;
	}

	public void SetSafehouseEnabled(bool enabled)
	{
		if (enabled != SafehouseEnabled)
		{
			SafehouseEnabled = enabled;
			FakeItem.Flags.MoveModeDisabled = enabled;
			FakeItem.Flags.MoveModeDisabledReason = "(Active Safehouse)";
			FakeItem.Flags.ReclaimInteractionDisabled = enabled;
			FakeItem.Flags.ReclaimInteractionDisabledReason = "(Active Safehouse)";
			if (enabled)
			{
				AddonData.AddProfileId();
			}
			else
			{
				HCSession.Instance.CustomSafehouseExfil.SetCustomExfilEnabled(enabled: false);
				AddonData.RemoveProfileId();
			}
			SafehouseEnabledStatePacket.Instance.Send(((LootItem)FakeItem.LootItem).ItemId, enabled);
			NotificationManager.DisplayMessageNotification($"Safehouse Enabled: {SafehouseEnabled}", ENotificationDurationType.Long, ENotificationIconType.Default, null);
		}
	}
}

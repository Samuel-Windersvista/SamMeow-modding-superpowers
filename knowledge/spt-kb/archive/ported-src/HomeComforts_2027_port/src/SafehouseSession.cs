using System;
using System.Collections.Generic;
using System.Linq;
using CommonAssets.Scripts.Game;
using EFT;
using EFT.Interactive;
using HomeComforts;
using HomeComforts.Components;
using HomeComforts.Helpers;
using HomeComforts.Items.Safehouse;
using LeaveItThere.Addon;
using LeaveItThere.Components;
using UnityEngine;

internal class SafehouseSession
{
	public class SafehouseProfileDataToHostPacket : LITPacketRegistration
	{
		public class Data
		{
			public Vector3 InfilPosition;

			public string SafehouseId;

			public bool RemoveProfile;
		}

		public static SafehouseProfileDataToHostPacket Instance => LITPacketRegistration.Get<SafehouseProfileDataToHostPacket>();

		public override EPacketDestination Destination => EPacketDestination.HostOnly;

		public override void OnPacketReceived(Packet packet)
		{
			Data data = packet.GetData<Data>();
			if (data.RemoveProfile)
			{
				HCSession.Instance.SafehouseSession.AddonData.RemoveProfile(packet.SenderProfileId);
			}
			else
			{
				HCSession.Instance.SafehouseSession.AddonData.AddProfile(packet.SenderProfileId, data.InfilPosition, data.SafehouseId);
			}
			Plugin.DebugLog("eor pak received");
		}

		public void Send(bool removeProfile)
		{
			if (!LITFikaTools.IAmHost() && (removeProfile || HCSession.Instance.CustomSafehouseExfil.LastSafehouseThatUsedMe != null))
			{
				Data data = new Data();
				if (removeProfile)
				{
					data.InfilPosition = Vector3.zero;
					data.SafehouseId = "n/a";
					data.RemoveProfile = true;
				}
				else
				{
					data.InfilPosition = HCSession.Instance.CustomSafehouseExfil.transform.position;
					data.SafehouseId = ((LootItem)HCSession.Instance.CustomSafehouseExfil.LastSafehouseThatUsedMe.FakeItem.LootItem).ItemId;
					data.RemoveProfile = false;
				}
				SendData(data);
			}
		}
	}

	private SafehouseGlobalAddonData _addonData;

	private static SafehouseSession _session => HCSession.Instance.SafehouseSession;

	public List<Safehouse> EnabledSafehouses { get; private set; } = new List<Safehouse>();

	public SafehouseGlobalAddonData AddonData
	{
		get
		{
			if (_addonData == null)
			{
				_addonData = LITSession.Instance.GetGlobalAddonDataOrNull<SafehouseGlobalAddonData>("Jehree.HomeComforts");
			}
			if (_addonData == null)
			{
				SafehouseGlobalAddonData safehouseGlobalAddonData = new SafehouseGlobalAddonData();
				LITSession.Instance.PutGlobalAddonData("Jehree.HomeComforts", safehouseGlobalAddonData);
				_addonData = safehouseGlobalAddonData;
			}
			return _addonData;
		}
	}

	public bool SafehouseEnableAllowed
	{
		get
		{
			int num = 0;
			foreach (Safehouse enabledSafehouse in EnabledSafehouses)
			{
				if (enabledSafehouse.SafehouseEnabled)
				{
					num++;
				}
			}
			return Settings.ThisMapSafehouseLimit > num;
		}
	}

	public Safehouse GetSafehouseOrNull(string itemId)
	{
		return EnabledSafehouses.FirstOrDefault((Safehouse i) => i.FakeItem.ItemId == itemId);
	}

	public bool SafehouseExists(string itemId)
	{
		return GetSafehouseOrNull(itemId) != null;
	}

	public void RemoveSafehouse(Safehouse safehouse)
	{
		EnabledSafehouses.Remove(safehouse);
	}

	public void AddSafehouse(Safehouse safehouse)
	{
		if (EnabledSafehouses.Contains(safehouse))
		{
			throw new Exception("Tried to add a Safehouse to HomeComfortsSession when it already existed in the list!");
		}
		EnabledSafehouses.Add(safehouse);
	}

	public static void OnRaidEnd(LocalRaidSettings settings, object results, object lostInsuredItems, object transferItems, string exitName)
	{
		bool flag = exitName != "homecomforts_safehouse" && !Settings.AlwaysInfilAtSafehouse.Value;
		if (exitName == "homecomforts_safehouse")
		{
			_session.AddonData.AddProfile();
		}
		if (flag)
		{
			_session.AddonData.RemoveProfile();
		}
		SafehouseProfileDataToHostPacket.Instance.Send(flag);
	}

	public static void InitializeCustomExfil(ExfiltrationController exfilController)
	{
		HCSession.Instance.CustomSafehouseExfil = SafehouseExfil.Create("homecomforts_safehouse");
		HCSession.Instance.CustomSafehouseExfil.transform.position = HCSession.Instance.InitialExfilPosition;
		HCSession.Instance.CustomSafehouseExfil.InitCustomExfil();
		ExfiltrationPoint[] exfiltrationPoints = exfilController.ExfiltrationPoints;
		ExfiltrationPoint[] array = new ExfiltrationPoint[1 + exfiltrationPoints.Length];
		for (int i = 0; i < exfiltrationPoints.Length; i++)
		{
			array[i] = exfiltrationPoints[i];
		}
		array[array.Length - 1] = HCSession.Instance.CustomSafehouseExfil;
		exfilController.ExfiltrationPoints = array;
	}

	public static void OnLastPlacedItemSpawned(FakeItem fakeItem)
	{
		if (_session.AddonData.ContainsProfile())
		{
			SafehouseGlobalAddonData.ProfileData profile = _session.AddonData.GetProfile();
			Safehouse safehouse = _session.GetSafehouseOrNull(profile.SafehouseId);
			if (safehouse != null && safehouse.SafehouseEnabled)
			{
				HCSession.Instance.Player.Teleport(profile.InfilPosition, false);
				return;
			}
			_session.AddonData.RemoveProfile();
			SafehouseProfileDataToHostPacket.Instance.Send(removeProfile: true);
		}
	}
}

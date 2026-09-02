using System;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using LeaveItThere.Addon;
using LeaveItThere.Components;
using UnityEngine;

namespace LeaveItThere.Fika;

internal class FikaBridge
{
	public delegate void SimpleEvent();

	public delegate bool SimpleBoolReturnEvent();

	public delegate string SimpleStringReturnEvent();

	public delegate void SendPlacedStateChangedPacketEvent(FakeItem fakeItem, bool isPlaced, bool physicsEnableRequested = false);

	public delegate void SendSpawnItemPacketEvent(Item item, Vector3 position, Quaternion rotation, Action<LootItem> senderCallback);

	public delegate void RegisterPacketEvent(LITPacketRegistration registration);

	public delegate void UnregisterPacketEvent(string packetGUID);

	public delegate void SendPacketEvent(LITPacketRegistration.Packet abstractedPacket);

	public static event SimpleEvent PluginEnableEmitted;

	public static event SimpleBoolReturnEvent IAmHostEmitted;

	public static event SimpleStringReturnEvent GetRaidIdEmitted;

	public static event SendPlacedStateChangedPacketEvent SendPlacedStateChangedPacketEmitted;

	public static event SendSpawnItemPacketEvent SendSpawnItemPacketEmitted;

	public static event RegisterPacketEvent RegisterPacketEmitted;

	public static event UnregisterPacketEvent UnregisterPacketEmitted;

	public static event SendPacketEvent SendPacketEmitted;

	public static void PluginEnable()
	{
		FikaBridge.PluginEnableEmitted?.Invoke();
	}

	public static bool IAmHost()
	{
		bool? flag = FikaBridge.IAmHostEmitted?.Invoke();
		if (!flag.HasValue)
		{
			return true;
		}
		return flag.Value;
	}

	public static string GetRaidId()
	{
		string text = FikaBridge.GetRaidIdEmitted?.Invoke();
		if (text == null)
		{
			return Singleton<GameWorld>.Instance.MainPlayer.ProfileId;
		}
		return text;
	}

	public static void SendPlacedStateChangedPacket(FakeItem fakeItem, bool isPlaced, bool physicsEnableRequested = false)
	{
		FikaBridge.SendPlacedStateChangedPacketEmitted?.Invoke(fakeItem, isPlaced, physicsEnableRequested);
	}

	public static void SendSpawnItemPacket(Item item, Vector3 position, Quaternion rotation, Action<LootItem> senderCallback)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		FikaBridge.SendSpawnItemPacketEmitted?.Invoke(item, position, rotation, senderCallback);
	}

	public static void RegisterPacket(LITPacketRegistration registration)
	{
		FikaBridge.RegisterPacketEmitted?.Invoke(registration);
	}

	public static void UnregisterPacket(string packetGUID)
	{
		FikaBridge.UnregisterPacketEmitted?.Invoke(packetGUID);
	}

	public static void SendPacket(LITPacketRegistration.Packet abstractedPacket)
	{
		FikaBridge.SendPacketEmitted?.Invoke(abstractedPacket);
	}
}

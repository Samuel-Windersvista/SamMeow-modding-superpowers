using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using LeaveItThere.Fika;
using Newtonsoft.Json;

namespace LeaveItThere.Addon;

public abstract class LITPacketRegistration
{
	public struct Packet
	{
		internal string PacketGUID = null;

		internal EPacketDestination Destination = EPacketDestination.Everyone;

		public string JsonData = null;

		public string SenderProfileId { get; internal set; } = null;

		public Packet()
		{
		}

		public T GetData<T>()
		{
			return JsonConvert.DeserializeObject<T>(JsonData);
		}
	}

	private static Dictionary<Type, LITPacketRegistration> _instances = new Dictionary<Type, LITPacketRegistration>();

	internal string PacketGUID => GetType().Namespace + "." + GetType().Name;

	public virtual EPacketDestination Destination => EPacketDestination.Everyone;

	protected LITPacketRegistration()
	{
		Type type = GetType();
		if (_instances.ContainsKey(type))
		{
			throw new InvalidOperationException(type.Name + " is a singleton and an instance already exists! Do not instantiate LITPacketRegistration derivatives. Get them with LITPacketRegistration.Get<YourPacketClassName>().");
		}
		_instances[type] = this;
	}

	public static T Get<T>() where T : LITPacketRegistration, new()
	{
		Type typeFromHandle = typeof(T);
		if (!_instances.ContainsKey(typeFromHandle))
		{
			_instances[typeFromHandle] = new T();
		}
		return (T)_instances[typeFromHandle];
	}

	public abstract void OnPacketReceived(Packet packet);

	protected virtual void OnPacketSent(Packet packet)
	{
	}

	public void Register()
	{
		FikaBridge.RegisterPacket(this);
	}

	public void Unregister()
	{
		FikaBridge.UnregisterPacket(PacketGUID);
	}

	internal void SendPacket(Packet packet)
	{
		packet.SenderProfileId = Singleton<GameWorld>.Instance.MainPlayer.ProfileId;
		packet.PacketGUID = PacketGUID;
		packet.Destination = Destination;
		OnPacketSent(packet);
		FikaBridge.SendPacket(packet);
	}

	public void SendData(object data)
	{
		if (Plugin.FikaInstalled && (!FikaBridge.IAmHost() || Destination != EPacketDestination.HostOnly))
		{
			Packet packet = new Packet();
			packet.JsonData = JsonConvert.SerializeObject(data);
			Packet packet2 = packet;
			SendPacket(packet2);
		}
	}
}

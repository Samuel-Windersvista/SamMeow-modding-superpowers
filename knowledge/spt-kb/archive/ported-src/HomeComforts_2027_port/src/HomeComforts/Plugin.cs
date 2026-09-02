using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HomeComforts.Components;
using HomeComforts.Helpers;
using HomeComforts.Items.Safehouse;
using HomeComforts.Items.SpaceHeater;
using HomeComforts.Patches;
using LeaveItThere.Addon;
using LeaveItThere.Helpers;
using SPT.Reflection.Patching;

namespace HomeComforts;

[BepInDependency("com.SPT.core", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("Jehree.LeaveItThere", "2.0.1")]
[BepInPlugin("Jehree.HomeComforts", "HomeComforts", "1.0.3")]
public class Plugin : BaseUnityPlugin
{
	public static ManualLogSource LogSource;

	public static ServerConfig ServerConfig;

	public static List<string> AllEntryPoints;

	public const string ConfigToClient = "/jehree/home_comforts/config_to_client";

	public const string GetAllEntryPoints = "/jehree/home_comforts/get_all_entry_points";

	public const string AddonDataKey = "Jehree.HomeComforts";

	private void Awake()
	{
		ServerConfig = LITUtils.ServerRoute<ServerConfig>(ConfigToClient);
		AllEntryPoints = LITUtils.ServerRoute<List<string>>(GetAllEntryPoints);
		LogSource = Logger;
		Settings.Init(Config);
		new InitAllExfiltrationPointsPatch().Enable();
		LITStaticEvents.OnFakeItemInitialized += Safehouse.OnFakeItemInitialized;
		LITStaticEvents.OnFakeItemInitialized += SpaceHeater.OnFakeItemInitialized;
		LITStaticEvents.OnLastPlacedItemSpawned += HCSession.OnLastPlacedItemSpawned;
		LITStaticEvents.OnRaidEnd += HCSession.OnRaidEnd;
		SpaceHeater.SpaceHeaterStatePacket.Instance.Register();
		Safehouse.SafehouseEnabledStatePacket.Instance.Register();
		SafehouseSession.SafehouseProfileDataToHostPacket.Instance.Register();
	}

	public static void DebugLog(string message)
	{
	}
}

public struct ServerConfig
{
	public List<string> SafehouseItemIds;

	public List<string> SpaceHeaterItemIds;
}

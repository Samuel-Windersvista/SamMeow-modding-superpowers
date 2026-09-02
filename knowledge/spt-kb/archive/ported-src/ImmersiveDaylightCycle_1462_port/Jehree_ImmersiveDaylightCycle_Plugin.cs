using System;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using EFT.UI;
using ImmersiveDaylightCycle.Common;
using Jehree.ImmersiveDaylightCycle.Helpers;
using Jehree.ImmersiveDaylightCycle.Patches;
using SPT.Reflection.Patching;

namespace Jehree.ImmersiveDaylightCycle;

[BepInPlugin("Jehree.ImmersiveDaylightCycle", "Jehree.ImmersiveDaylightCycle", "2.2.0")]
public class Plugin : BaseUnityPlugin
{
	public static ManualLogSource LogSource { get; private set; }

	public static bool FikaInstalled { get; private set; }

	public static bool IAmDedicatedClient { get; private set; }

	private void Awake()
	{
		FikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
		IAmDedicatedClient = Chainloader.PluginInfos.ContainsKey("com.fika.dedicated");
		LogSource = Logger;
		Settings.Init(((BaseUnityPlugin)this).Config);
		if (!IAmDedicatedClient)
		{
			((ModulePatch)new TimeUIPanelPatch()).Enable();
			((ModulePatch)new LocationConditionsPanelPatch()).Enable();
			((ModulePatch)new TimeUIUpdatePatch()).Enable();
		}
		((ModulePatch)new OfflineRaidEndedPatch()).Enable();
		((ModulePatch)new OnGameStartedPatch()).Enable();
		ConsoleScreen.Processor.RegisterCommandGroup<CommandGroup>();
		TryInitFikaModuleAssembly();
	}

	private void TryInitFikaModuleAssembly()
	{
		if (FikaInstalled)
		{
			Assembly assembly = Assembly.Load("ImmersiveDaylightCycle-FikaModule");
			Type type = assembly.GetType("ImmersiveDaylightCycle.FikaModule.Main");
			MethodInfo method = type.GetMethod("Init");
			method.Invoke(type, null);
		}
	}
}

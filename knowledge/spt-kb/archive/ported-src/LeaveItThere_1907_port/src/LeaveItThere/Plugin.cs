using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using EFT.UI;
using Helpers.CursorHelper;
using LeaveItThere.Common;
using LeaveItThere.Fika;
using LeaveItThere.Helpers;
using LeaveItThere.Patches;
using Newtonsoft.Json;
using SPT.Reflection.Patching;

namespace LeaveItThere;

[BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("Jehree.LeaveItThere", "LeaveItThere", "2.0.1")]
public class Plugin : BaseUnityPlugin
{
	public const string DataToServerURL = "/jehree/pip/data_to_server";

	public const string DataToClientURL = "/jehree/pip/data_to_client";

	public static ManualLogSource LogSource;

	private static string _assemblyPath = Assembly.GetExecutingAssembly().Location;

	public static string AssemblyFolderPath = Path.GetDirectoryName(_assemblyPath);

	private static string _itemFilterPath = Path.Combine(AssemblyFolderPath, "placeable_item_filter.json");

	public static bool FikaInstalled { get; private set; }

	internal static ItemFilter PlaceableItemFilter { get; private set; }

	private void Awake()
	{
		FikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");
		LogSource = Logger;
		if (File.Exists(_itemFilterPath))
		{
			PlaceableItemFilter = JsonConvert.DeserializeObject<ItemFilter>(File.ReadAllText(_itemFilterPath));
		}
		else
		{
			PlaceableItemFilter = new ItemFilter();
			string contents = JsonConvert.SerializeObject((object)PlaceableItemFilter);
			File.WriteAllText(_itemFilterPath, contents);
		}
		PlaceableItemFilter.BuildLookups();
		Settings.Init(((BaseUnityPlugin)this).Config);
		LogSource.LogInfo((object)"Ebu is cute :3");
		if (FikaInstalled)
		{
			((ModulePatch)new EarlyGameStartedPatchFika()).Enable();
		}
		else
		{
			((ModulePatch)new EarlyGameStartedPatch()).Enable();
		}
		((ModulePatch)new GetAvailableActionsPatch()).Enable();
		((ModulePatch)new GameEndedPatch()).Enable();
		((ModulePatch)new InteractionsChangedHandlerPatch()).Enable();
		((ModulePatch)new LootExperiencePatch()).Enable();
		((ModulePatch)new CursorHelper.CursorPatch()).Enable();
		ConsoleScreen.Processor.RegisterCommandGroup<ConsoleCommands>();
		TryInitFikaModuleAssembly();
	}

	private void OnEnable()
	{
		FikaBridge.PluginEnable();
		BundleThings.LoadBundles();
	}

	private void TryInitFikaModuleAssembly()
	{
		if (!FikaInstalled)
		{
			return;
		}
		try
		{
			Type type = Assembly.Load("LeaveItThere-FikaModule").GetType("LeaveItThere.FikaModule.Main");
			type.GetMethod("Init").Invoke(type, null);
		}
		catch (Exception e)
		{
			LogSource.LogError("Failed to init LeaveItThere-FikaModule: " + e.Message);
		}
	}

	public static void DebugLog(string message)
	{
	}
}

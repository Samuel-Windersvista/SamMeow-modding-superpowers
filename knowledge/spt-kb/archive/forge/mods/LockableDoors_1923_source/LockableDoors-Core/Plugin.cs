using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using EFT.UI;
using LockableDoors.Common;
using LockableDoors.Fika;
using LockableDoors.Helpers;
using LockableDoors.Patches;
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LockableDoors-FikaModule")]

namespace LockableDoors
{
    [BepInDependency("xyz.drakia.doorrandomizer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin("Jehree.LockableDoors", "LockableDoors", "2.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string DataToServerURL = "/jehree/lockabledoors/data_to_server";
        public const string DataToClientURL = "/jehree/lockabledoors/data_to_client";

        public static bool FikaInstalled { get; private set; }
        public static ManualLogSource LogSource;


        private void Awake()
        {
            FikaInstalled = Chainloader.PluginInfos.ContainsKey("com.fika.core");

            LogSource = Logger;
            Settings.Init(Config);
            LogSource.LogWarning("Ebu is cute :3");

            new GetAvailableActionsPatch().Enable();
            new GameStartedPatch().Enable();
            new GameEndedPatch().Enable();

            ConsoleScreen.Processor.RegisterCommandGroup<ConsoleCommands>();

            TryInitFikaModuleAssembly();
        }

        private void OnEnable()
        {
            FikaBridge.PluginEnable();
        }

        private void TryInitFikaModuleAssembly()
        {
            if (!FikaInstalled) return;

            Assembly fikaModuleAssembly = Assembly.Load("LockableDoors-FikaModule");
            Type main = fikaModuleAssembly.GetType("LockableDoors.FikaModule.Main");
            MethodInfo init = main.GetMethod("Init");

            init.Invoke(main, null);
        }
    }
}
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using CustomPlayerLoopSystem;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace SamSWAT.HeliCrash.ArysReloaded;

public class InitializeApplicationLifetimeScopePatch : ModulePatch
{
    private static BaseUnityPlugin s_unityPlugin;
    private static ManualLogSource s_logger;
    private static GameObject s_bepinexManager;

    public InitializeApplicationLifetimeScopePatch(
        BaseUnityPlugin unityPlugin,
        ManualLogSource logger,
        GameObject bepinexManager
    )
    {
        s_unityPlugin = unityPlugin;
        s_logger = logger;
        s_bepinexManager = bepinexManager;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(CustomPlayerLoopSystemsInjector),
            nameof(CustomPlayerLoopSystemsInjector.Injection)
        );
    }

    [PatchPostfix]
    private static void PatchPostfix()
    {
        var go = new GameObject("HeliCrash_AppLifetimeScope");
        go.transform.SetParent(s_bepinexManager.transform);
        var appLifetimeScope = go.AddComponent<ApplicationLifetimeScope>();
        appLifetimeScope.Initialize(s_unityPlugin, s_logger);
        appLifetimeScope.Build();

        s_unityPlugin = null;
        s_logger = null;
        s_bepinexManager = null;
    }
}

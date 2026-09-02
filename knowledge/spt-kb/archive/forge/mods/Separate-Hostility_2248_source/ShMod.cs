using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace SeparateHostility;

internal class ModInfo
{
    internal const string Guid = "dk.sptplugins.separatehostility";
    internal const string Name = "Separate Hostility";
    internal const string Version = "1.0.0";
}

[BepInPlugin(ModInfo.Guid, ModInfo.Name, ModInfo.Version)]
[BepInDependency("com.SPT.custom", "3.11.0")]
public class ShMod : BaseUnityPlugin
{
    private static ShMod? _instance;

    private void Awake()
    {
        _instance = this;

        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModInfo.Guid);
    }

    internal static void Log(object payload)
    {
        _instance?.Logger.LogInfo(payload);
    }
}
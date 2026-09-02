using System.Reflection;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;
using VContainer;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika.Bootstrapper;

public class OverrideRaidLifetimeScopePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RaidLifetimeScope), "Configure");
    }

    [PatchPrefix]
    private static bool PatchPrefix(IContainerBuilder builder)
    {
        if (FikaBackendUtils.IsServer)
        {
            builder
                .Register<ServerHeliCrashSpawner>(Lifetime.Scoped)
                .As<HeliCrashSpawner>()
                .AsSelf();

            return false;
        }

        if (FikaBackendUtils.IsClient)
        {
            builder
                .Register<ClientHeliCrashSpawner>(Lifetime.Scoped)
                .As<HeliCrashSpawner>()
                .AsSelf();

            return false;
        }

        return true;
    }
}

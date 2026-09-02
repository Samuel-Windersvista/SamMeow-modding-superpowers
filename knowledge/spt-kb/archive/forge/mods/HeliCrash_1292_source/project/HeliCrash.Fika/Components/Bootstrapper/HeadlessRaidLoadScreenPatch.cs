using System;
using System.Reflection;
using Fika.Core.Main.Utils;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamSWAT.HeliCrash.ArysReloaded.Fika.Bootstrapper;

public class HeadlessRaidLoadScreenPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(RaidLoadScreenPatch), "GetTargetMethod");
    }

    [PatchPrefix]
    private static bool PatchPrefix(ref MethodBase __result)
    {
        if (FikaBackendUtils.IsHeadless)
        {
            Type headlessGameType = AccessTools.TypeByName(
                "Fika.Headless.Classes.GameMode.HeadlessGame"
            );

            if (headlessGameType == null)
            {
                Logger.LogError(
                    "[SamSWAT.HeliCrash.ArysReloaded] Could not find HeadlessGame type via name!"
                );
            }

            __result = AccessTools.Method(headlessGameType, "LoadLoot");
            return false;
        }

        return true;
    }
}

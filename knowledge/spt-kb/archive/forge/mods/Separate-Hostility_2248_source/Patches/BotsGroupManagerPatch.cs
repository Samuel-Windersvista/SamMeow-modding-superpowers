using System.Collections.Generic;
using System.Reflection;
using EFT;
using HarmonyLib;
using SeparateHostility.Extensions;

namespace SeparateHostility.Patches;

[HarmonyPatch]
internal class BotsGroupManagerPatch
{
    internal static IEnumerable<MethodBase> TargetMethods()
    {
        return [
            AccessTools.Method(typeof(BotZoneGroupsDictionary), nameof(BotZoneGroupsDictionary.Add),
            [
                typeof(BotZone),
                typeof(EPlayerSide),
                typeof(BotsGroup),
                typeof(bool),
            ]),
            AccessTools.Method(typeof(BotZoneGroupsDictionary), nameof(BotZoneGroupsDictionary.AddNoKey)),
        ];
    }

    [HarmonyPrefix]
    internal static void OnInstantiate(BotZoneGroupsDictionary __instance, BotZone zone)
    {
        __instance.TryAdd(zone, new BotsGroupManager());
    }
}
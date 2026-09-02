using System.Collections.Generic;
using HarmonyLib;
using SeparateHostility.Extensions;

namespace SeparateHostility.Patches;

[HarmonyPatch]
internal class GetGroupsCombinedPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BotZoneGroups), nameof(BotZoneGroups.GetGroups), [typeof(bool)])]
    internal static void OnGetGroups(BotZoneGroups __instance, HashSet<BotsGroup> __result)
    {
        if (__instance is BotsGroupManager manager) {
            __result.UnionWith(manager._spawnGroups.Values);
        }
    }
}
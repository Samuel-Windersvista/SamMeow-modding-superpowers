using System.Collections.Generic;
using HarmonyLib;
using SeparateHostility.Extensions;

namespace SeparateHostility.Patches;

[HarmonyPatch]
internal class GetGroupsCombinedPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GClass555), nameof(GClass555.GetGroups))]
    internal static void OnGetGroups(GClass555 __instance, HashSet<BotsGroup> __result)
    {
        if (__instance is BotsGroupManager manager) {
            __result.UnionWith(manager._spawnGroups.Values);
        }
    }
}
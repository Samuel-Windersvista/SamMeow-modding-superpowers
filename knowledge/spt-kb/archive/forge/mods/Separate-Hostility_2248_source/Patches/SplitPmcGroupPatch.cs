using EFT;
using HarmonyLib;
using SeparateHostility.Extensions;

namespace SeparateHostility.Patches;

[HarmonyPatch]
internal class SplitPmcGroupPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BotSpawner), nameof(BotSpawner.GetGroupAndSetEnemies))]
    internal static bool OnSetPmcGroup(BotSpawner __instance, BotOwner bot, BotZone zone, ref BotsGroup __result)
    {
        if (bot.Profile.Info.Settings.Role is not (WildSpawnType.pmcBEAR or WildSpawnType.pmcUSEC)) {
            return true;
        }

        if (!__instance.Groups.TryGetValue(zone, out var value) || value is not BotsGroupManager manager) {
            __instance.Groups[zone] = manager = new();
        }

        var spawn = bot.SpawnProfileData.SpawnParams;
        if (!manager._spawnGroups.TryGetValue(spawn, out var group)) {
            group = manager._spawnGroups[spawn] = new(zone, __instance.BotGame, bot, [], __instance._deadBodiesController,
                __instance._allPlayers, true) {
                TargetMembersCount = bot.SpawnProfileData.SpawnParams.ShallBeGroup.StartCount,
            };
            ShMod.Log($"creating new group for {bot.Profile.Nickname} | {group.GetHashCode()}");
        }

        ShMod.Log($"+ adding {bot.Profile.Nickname} to group {group.GetHashCode()}");
        group.AddMember(bot, true);

        __result = group;
        return false;
    }
}
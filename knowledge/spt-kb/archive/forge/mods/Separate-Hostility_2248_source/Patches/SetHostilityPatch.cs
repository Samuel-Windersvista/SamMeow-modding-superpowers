using System.Linq;
using Comfort.Common;
using EFT;
using HarmonyLib;

namespace SeparateHostility.Patches;

[HarmonyPatch]
internal class SetHostilityPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(BotOwner), nameof(BotOwner.method_10))]
    internal static void OnPreActivate(BotOwner __instance)
    {
        switch (__instance.Profile.Info.Settings.Role) {
            // pmc
            case WildSpawnType.pmcBEAR:
            case WildSpawnType.pmcUSEC:
                SetAllAsEnemies(__instance);
                break;
            case WildSpawnType.assault:
            case WildSpawnType.assaultGroup:
            case WildSpawnType.cursedAssault:
            case WildSpawnType.crazyAssaultEvent:
            case WildSpawnType.marksman:
            // golden TT
            case WildSpawnType.bossBully:
            // mall dude
            case WildSpawnType.bossKilla:
            // streets couple
            case WildSpawnType.bossBoar:
            case WildSpawnType.followerBoar:
            case WildSpawnType.bossBoarSniper:
            case WildSpawnType.followerBoarClose1:
            case WildSpawnType.followerBoarClose2:
            case WildSpawnType.bossKolontay:
            case WildSpawnType.followerKolontayAssault:
            case WildSpawnType.followerKolontaySecurity:
            // SVDS camper
            case WildSpawnType.bossKojaniy:
            case WildSpawnType.followerKojaniy:
            // boy gang leader
            case WildSpawnType.bossGluhar:
            case WildSpawnType.followerGluharAssault:
            case WildSpawnType.followerGluharSecurity:
            case WildSpawnType.followerGluharScout:
            case WildSpawnType.followerGluharSnipe:
            // orange bags
            case WildSpawnType.bossSanitar:
            case WildSpawnType.followerSanitar:
            // orange bag wannabe
            case WildSpawnType.bossPartisan:
            // rogues
            case WildSpawnType.exUsec:
            // men in black
            case WildSpawnType.sectantWarrior:
            case WildSpawnType.sectantPriest:
            case WildSpawnType.sectantPredvestnik:
            case WildSpawnType.sectantPrizrak:
            case WildSpawnType.sectantOni:
            // hammer guy
            case WildSpawnType.bossTagilla:
            case WildSpawnType.followerTagilla:
            // threesome
            case WildSpawnType.bossKnight:
            case WildSpawnType.followerBigPipe:
            case WildSpawnType.followerBirdEye:
            // zombies
            case WildSpawnType.infectedAssault:
            case WildSpawnType.infectedPmc:
            case WildSpawnType.infectedCivil:
            case WildSpawnType.infectedLaborant:
            case WildSpawnType.infectedTagilla:
                SetPmcAsEnemies(__instance);
                break;
        }
    }

    private static void SetAllAsEnemies(BotOwner newBot)
    {
        var humanPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList
            .Where(p => !p.IsAI);
        foreach (var humanPlayer in humanPlayers) {
            newBot.BotsGroup.AddEnemy(humanPlayer, EBotEnemyCause.initial);
        }

        var activatedBots = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
            .Where(b => b.BotState is not (EBotState.ActiveFail or EBotState.Disposed or EBotState.NonActive) &&
                        b.Profile.Id != newBot.Profile.Id);
        foreach (var bot in activatedBots) {
            switch (bot.Profile.Info.Settings.Role) {
                case WildSpawnType.bossZryachiy:
                case WildSpawnType.followerZryachiy:
                case WildSpawnType.peacefullZryachiyEvent:
                case WildSpawnType.shooterBTR:
                case WildSpawnType.gifter:
                    continue;
            }

            bot.BotsGroup.AddEnemy(newBot, EBotEnemyCause.initial);
            newBot.BotsGroup.AddEnemy(bot, EBotEnemyCause.initial);
        }

        foreach (var sameGroupMember in newBot.BotsGroup._members) {
            newBot.BotsGroup.RemoveEnemy(sameGroupMember);
        }
    }

    private static void SetPmcAsEnemies(BotOwner newBot)
    {
        var humanPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList
            .Where(p => !p.IsAI);
        foreach (var humanPlayer in humanPlayers) {
            if (humanPlayer.Profile.Info.Side is EPlayerSide.Usec or EPlayerSide.Bear) {
                newBot.BotsGroup.AddEnemy(humanPlayer, EBotEnemyCause.initial);
            }
        }

        var activatedBots = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
            .Where(b => b.BotState is not (EBotState.ActiveFail or EBotState.Disposed or EBotState.NonActive));
        foreach (var bot in activatedBots) {
            var role = bot.Profile.Info.Settings.Role;
            if (role is not (WildSpawnType.pmcUSEC or WildSpawnType.pmcBEAR)) {
                continue;
            }

            bot.BotsGroup.AddEnemy(newBot, EBotEnemyCause.initial);
            newBot.BotsGroup.AddEnemy(bot, EBotEnemyCause.initial);
        }
    }
}
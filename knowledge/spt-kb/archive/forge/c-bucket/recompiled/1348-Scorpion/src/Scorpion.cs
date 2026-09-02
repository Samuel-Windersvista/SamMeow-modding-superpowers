// Scorpion 主类（反编译重写：4.0 → 4.1.2）
// 关键修改：
//   1. OnLoad() → OnLoadAsync(CancellationToken)（4.1 IOnLoad 签名）
//   2. ConfigServer 已删除 → TraderConfig/RagfairConfig 直接注入（4.1 配置模型可注入，参考 TraderHelper/RagfairOfferGenerator ctor）
//   3. DatabaseService 已删除 → TemplateTable/LocaleTable 表模型注入（GetTables().Templates.Quests → templateTable.Quests；GetLocales().Global → localeTable.Global）
//   4. QuestConditionCounterCondition.Weapon 由 List<string> 改为 HashSet<string>?（Add 仍兼容）
//   5. 空值防御：QuestName/Counter/Conditions/Weapon 可空，遍历改为 null 安全
using System.Globalization;
using System.Reflection;
using Path = System.IO.Path;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib;
using _scorpion.Models;

namespace _scorpion;

[Injectable(InjectionType.Scoped, 400420)]
public class Scorpion(
    ModHelper modHelper,
    ImageRouter imageRouter,
    TraderConfig traderConfig,
    RagfairConfig ragfairConfig,
    AddCustomTraderHelper addCustomTraderHelper,
    TemplateTable templateTable,
    LocaleTable localeTable,
    CustomDynamicRouter dynamicRouter,
    JsonUtil jsonUtil,
    global::WTTServerCommonLib.WTTServerCommonLib wttCommon) : IOnLoad
{
    private readonly TraderConfig _traderConfig = traderConfig;

    private readonly RagfairConfig _ragfairConfig = ragfairConfig;

    public ModConfig? Config;

    public string ScorpionId = "6688d464bc40c867f60e7d7e";

    private string _modPath = string.Empty;

    private static readonly Random _random = new();

    private readonly List<string> _loadMessage =
    [
        "Scorpion has brought his crew into Tarkov", "One of us..one of us..one of us", "Welcome to the team, you're one of us meow ♡", "Call Kenny Loggins because you're in the danger zone", "Can I offer you a nice egg in this trying time?", "Good news everyone! We have over 100 quests!", "Never half-ass two things. Whole-ass one thing.", "Thanks for signing up for Cat Facts! You will now receive fun daily facts about CATS!", "Thanks for signing up for Dog Facts! You will now receive fun daily facts about DOGS!", "A big ball of wibbly wobbly, timey wimey stuff",
        "(╯°□°)╯︵ ┻━┻ ", "┬─┬ノ( º _ ºノ)", "Treat others how you want to be treated", "No act of kindness, no matter how small, is ever wasted", "Reticulating Splines...", "Unfolding Foldy Chairs...", "Pressurizing Fruit Punch Barrel Hydraulics...", "Fabricating Imaginary Infrastructure...", "We apologize again for the fault in the subtitles. Those responsible for sacking the people who have just been sacked, have been sacked.", "Are you suggesting coconuts migrate?",
        "We are now the knights who say ekki-ekki-ekki-pitang-zoom-boing!", "Knight jumps queen! Bishop jumps queen! Pawns jump queen!", "Hello. My name is Inigo Montoya. You killed my father. Prepare to die.", "I spent the last few years building up an immunity to iocane powder.", "Rodents Of Unusual Size? I don't think they exist.", "Always try to be nice, but never fail to be kind", "Never be cruel, never be cowardly", "Who do I need to ban? (◣_◢)", "This loading message is sponsored by Raid: Shadow Legends"
    ];

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        _modPath = modHelper.GetAbsolutePathToModFolder(assembly);
        Config = await jsonUtil.DeserializeFromFileAsync<ModConfig>(Path.Combine(_modPath, "config.json"), cancellationToken);
        BuildTraderAndAssort();
        dynamicRouter.PassConfig(Config);
        AddQuestsToDatabase();
        ModdedWeaponCompatibility();
        await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly, Path.Combine("data", "zones"));
        string text = _loadMessage[_random.Next(_loadMessage.Count)];
        Console.WriteLine("[SCORPION] " + text);
    }

    private void BuildTraderAndAssort()
    {
        string text = Path.Combine(_modPath, "data", ScorpionId + ".jpg");
        TraderBase val = jsonUtil.DeserializeFromFile<TraderBase>(Path.Combine(_modPath, "data", "base.json"));
        TraderAssort traderAssort = jsonUtil.DeserializeFromFile<TraderAssort>(Path.Combine(_modPath, "data", "assort.json"));
        Dictionary<string, Dictionary<MongoId, MongoId>> newAssorts = jsonUtil.DeserializeFromFile<Dictionary<string, Dictionary<MongoId, MongoId>>>(Path.Combine(_modPath, "data", "questassort.json"));
        List<HideoutProduction> newProductions = jsonUtil.DeserializeFromFile<List<HideoutProduction>>(Path.Combine(_modPath, "data", "production.json"));
        string text2 = Path.Combine(_modPath, "data", "scorpion_questpic.png");
        DateTime dateTime = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
        if (dateTime.Month == 3 && dateTime.Day == 1)
        {
            text = Path.Combine(_modPath, "data", ScorpionId + "_aprilfools.jpg");
            val.Nickname = "ScorpionXYZ";
            val.Name = "ScorpionXYZ";
        }
        traderAssort = AdjustTraderAssort(traderAssort);
        imageRouter.AddRoute(val.Avatar!.Replace(".jpg", ""), text);
        imageRouter.AddRoute("/files/quest/icon/" + Path.GetFileNameWithoutExtension(text2), text2);
        addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, val, Config!.TraderRefreshMin, Config.TraderRefreshMax);
        _ragfairConfig.Traders.TryAdd(val.Id, Config.AddTraderToFlea);
        addCustomTraderHelper.AddTraderWithEmptyAssortToDb(val);
        addCustomTraderHelper.OverwriteTraderAssort(val.Id, traderAssort);
        addCustomTraderHelper.OverwriteTraderQuestAssort(val.Id, newAssorts);
        addCustomTraderHelper.AddTraderProductions(val.Id, newProductions);
        addCustomTraderHelper.AddTraderToLocales(val, "Scorpion", "I'm sellin', what are you buyin'?");
    }

    private TraderAssort AdjustTraderAssort(TraderAssort traderAssort)
    {
        foreach (var barterScheme in traderAssort.BarterScheme)
        {
            foreach (List<BarterScheme> barterSchemeItems in barterScheme.Value)
            {
                foreach (BarterScheme item in barterSchemeItems)
                {
                    if (item.Template == ItemTpl.MONEY_ROUBLES || item.Template == ItemTpl.MONEY_DOLLARS || item.Template == ItemTpl.MONEY_EUROS)
                    {
                        item.Count = Math.Round(item.Count!.Value * Config!.PriceMultiplier);
                    }
                }
            }
        }
        if (Config!.RemoveLoyaltyRestriction)
        {
            foreach (var (key, _) in traderAssort.LoyalLevelItems)
            {
                traderAssort.LoyalLevelItems[key] = 1;
            }
        }
        return traderAssort;
    }

    private void AddQuestsToDatabase()
    {
        Dictionary<MongoId, Quest> jsonDataFromFile = modHelper.GetJsonDataFromFile<Dictionary<MongoId, Quest>>(Path.Combine(_modPath, "data", "quests"), "Scorpion_quests.json");
        Dictionary<MongoId, StartEndDate> jsonDataFromFile2 = modHelper.GetJsonDataFromFile<Dictionary<MongoId, StartEndDate>>(Path.Combine(_modPath, "data", "eventdates"), "Scorpion_events.json");
        Dictionary<MongoId, Quest> quests = templateTable.Quests;
        foreach (var (key, value) in jsonDataFromFile)
        {
            DateTime dateTime = new(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);
            if (jsonDataFromFile2.TryGetValue(key, out var value3))
            {
                DateTime dateTime2 = new(dateTime.Year, value3.StartMonth, value3.StartDay);
                DateTime dateTime3 = new(dateTime.Year, value3.EndMonth, value3.EndDay);
                bool flag = dateTime3 < dateTime2 ? dateTime >= dateTime2 || dateTime <= dateTime3 : dateTime >= dateTime2 && dateTime <= dateTime3;
                if (!Config!.EventQuestsAlwaysActive && !flag)
                {
                    continue;
                }
            }
            quests[key] = value;
        }
        string[] directories = Directory.GetDirectories(Path.Combine(_modPath, "data", "locales"));
        List<string> list = new();
        Dictionary<string, string> questLocaleData = new();
        string culture = CultureInfo.CurrentUICulture.Name.ToLowerInvariant();
        string[] array = directories;
        foreach (string path in array)
        {
            string fileName = Path.GetFileName(path);
            list.Add(fileName);
            string[] files = Directory.GetFiles(path);
            foreach (string text in files)
            {
                Dictionary<string, string> dictionary = jsonUtil.DeserializeFromFile<Dictionary<string, string>>(text) ?? new Dictionary<string, string>();
                if (fileName == "es-mx")
                {
                    list.Add("es");
                    AddToLocales(dictionary, "es");
                }
                if (fileName is "zh-CN" or "zh-TW")
                {
                    if (MatchesCulture(culture, fileName))
                    {
                        AddToLocales(dictionary, "ch");
                    }
                    continue;
                }
                AddToLocales(dictionary, fileName);
                if (fileName == "en")
                {
                    questLocaleData = dictionary;
                }
            }
        }
        foreach (var (text3, _) in localeTable.Global)
        {
            if (!list.Contains(text3))
            {
                AddToLocales(questLocaleData, text3);
            }
        }
    }

    private bool MatchesCulture(string culture, string localeFile)
    {
        if ((culture.StartsWith("zh-cn") || culture.StartsWith("zh-sg") || culture.Contains("hans", StringComparison.OrdinalIgnoreCase)) && localeFile == "zh-CN")
        {
            return true;
        }
        if ((culture.StartsWith("zh-tw") || culture.StartsWith("zh-hk") || culture.StartsWith("zh-mo") || culture.Contains("hant", StringComparison.OrdinalIgnoreCase)) && localeFile == "zh-TW")
        {
            return true;
        }
        return false;
    }

    private void AddToLocales(Dictionary<string, string> questLocaleData, string locale)
    {
        if (!localeTable.Global.TryGetValue(locale, out var value))
        {
            return;
        }
        value.AddTransformer(localeData =>
        {
            if (localeData is null)
            {
                return localeData;
            }
            foreach (var (key, value2) in questLocaleData)
            {
                localeData[key] = value2;
            }
            return localeData;
        });
    }

    private void ModdedWeaponCompatibility()
    {
        ModdedWeaponCompatibility jsonDataFromFile = modHelper.GetJsonDataFromFile<ModdedWeaponCompatibility>(_modPath, "ModdedWeaponCompatibility.json");
        List<Quest> source = templateTable.Quests.Values.ToList();
        if (jsonDataFromFile.AssaultRifles?.Count >= 1)
        {
            List<string> assaultRifles = jsonDataFromFile.AssaultRifles;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - ARs") == true).ToList();
            ModdedWeaponPushToArray(questTable, assaultRifles);
        }
        if (jsonDataFromFile.SubmachineGuns?.Count >= 1)
        {
            List<string> submachineGuns = jsonDataFromFile.SubmachineGuns;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - SMGs") == true).ToList();
            ModdedWeaponPushToArray(questTable, submachineGuns);
        }
        if (jsonDataFromFile.Snipers?.Count >= 1)
        {
            List<string> snipers = jsonDataFromFile.Snipers;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - Snipers") == true).ToList();
            ModdedWeaponPushToArray(questTable, snipers);
        }
        if (jsonDataFromFile.Marksman?.Count >= 1)
        {
            List<string> marksman = jsonDataFromFile.Marksman;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - Marksman") == true).ToList();
            ModdedWeaponPushToArray(questTable, marksman);
        }
        if (jsonDataFromFile.Shotguns?.Count >= 1)
        {
            List<string> shotguns = jsonDataFromFile.Shotguns;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - Shotguns") == true).ToList();
            ModdedWeaponPushToArray(questTable, shotguns);
        }
        if (jsonDataFromFile.Pistols?.Count >= 1)
        {
            List<string> pistols = jsonDataFromFile.Pistols;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - Pistols") == true).ToList();
            ModdedWeaponPushToArray(questTable, pistols);
        }
        if (jsonDataFromFile.LargeMachineGuns?.Count >= 1)
        {
            List<string> largeMachineGuns = jsonDataFromFile.LargeMachineGuns;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - LMGs") == true).ToList();
            ModdedWeaponPushToArray(questTable, largeMachineGuns);
        }
        if (jsonDataFromFile.Carbines?.Count >= 1)
        {
            List<string> carbines = jsonDataFromFile.Carbines;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - Carbines") == true).ToList();
            ModdedWeaponPushToArray(questTable, carbines);
        }
        if (jsonDataFromFile.Melee?.Count >= 1)
        {
            List<string> melee = jsonDataFromFile.Melee;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - Melee") == true).ToList();
            ModdedWeaponPushToArray(questTable, melee);
        }
        if (jsonDataFromFile.Explosives?.Count >= 1)
        {
            List<string> explosives = jsonDataFromFile.Explosives;
            List<Quest> questTable = source.Where((Quest x) => x.QuestName?.Contains("Weapon Proficiency - Explosives") == true).ToList();
            ModdedWeaponPushToArray(questTable, explosives);
        }
    }

    private void ModdedWeaponPushToArray(List<Quest> questTable, List<string> weaponType)
    {
        foreach (Quest item in questTable)
        {
            var availableForFinish = item.Conditions.AvailableForFinish;
            if (availableForFinish is null)
            {
                continue;
            }
            foreach (QuestCondition item2 in availableForFinish)
            {
                var counterConditions = item2.Counter?.Conditions;
                if (counterConditions is null)
                {
                    continue;
                }
                foreach (QuestConditionCounterCondition condition in counterConditions)
                {
                    if (condition.Weapon is null)
                    {
                        continue;
                    }
                    foreach (string item3 in weaponType)
                    {
                        condition.Weapon.Add(item3);
                    }
                }
            }
        }
    }
}

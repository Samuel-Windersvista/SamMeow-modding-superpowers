// AddCustomTraderHelper（反编译重写：4.0 → 4.1.2）
// 关键修改：DatabaseService（GetTables().Traders/Locales/Hideout）→ TradersTable/LocaleTable/HideoutTable 表模型注入
// 参考 Artem 4.1.2 模式：((Dictionary<MongoId, Trader>)(object)tradersTable).TryAdd / localeTable.Global.AddTransformer
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;

namespace _scorpion;

[Injectable(InjectionType.Scoped, 400001)]
public class AddCustomTraderHelper(
    ISptLogger<AddCustomTraderHelper> logger,
    ICloner cloner,
    TradersTable tradersTable,
    LocaleTable localeTable,
    HideoutTable hideoutTable)
{
    public void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
    {
        UpdateTime item = new()
        {
            TraderId = baseJson.Id,
            Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
        };
        traderConfig.UpdateTime.Add(item);
    }

    public void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
    {
        var assort = new TraderAssort
        {
            Items = new List<Item>(),
            BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
            LoyalLevelItems = new Dictionary<MongoId, int>()
        };
        var trader = new Trader
        {
            Assort = assort,
            Base = cloner.Clone<TraderBase>(traderDetailsToAdd),
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
            {
                { "Started", new Dictionary<MongoId, MongoId>() },
                { "Success", new Dictionary<MongoId, MongoId>() },
                { "Fail", new Dictionary<MongoId, MongoId>() }
            },
            Dialogue = new Dictionary<string, List<string>>()
        };
        ((Dictionary<MongoId, Trader>)(object)tradersTable).TryAdd(traderDetailsToAdd.Id, trader);
    }

    public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
    {
        var global = localeTable.Global;
        MongoId newTraderId = baseJson.Id;
        string fullName = baseJson.Name;
        string nickName = baseJson.Nickname;
        string location = baseJson.Location;
        foreach (var (_, lazyLocale) in global)
        {
            lazyLocale.AddTransformer(localeData =>
            {
                if (localeData is null)
                {
                    return localeData;
                }
                localeData[$"{newTraderId} FullName"] = fullName;
                localeData[$"{newTraderId} FirstName"] = firstName;
                localeData[$"{newTraderId} Nickname"] = nickName;
                localeData[$"{newTraderId} Location"] = location;
                localeData[$"{newTraderId} Description"] = description;
                return localeData;
            });
        }
    }

    public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
    {
        if (!((Dictionary<MongoId, Trader>)(object)tradersTable).TryGetValue(traderId, out var trader))
        {
            logger.Warning("Unable to update assorts for trader: " + traderId + ", they couldn't be found on the server");
            return;
        }
        trader.Assort = newAssorts;
    }

    public void OverwriteTraderQuestAssort(string traderId, Dictionary<string, Dictionary<MongoId, MongoId>> newAssorts)
    {
        if (!((Dictionary<MongoId, Trader>)(object)tradersTable).TryGetValue(traderId, out var trader))
        {
            logger.Warning("Unable to update quest assorts for trader: " + traderId + ", they couldn't be found on the server");
            return;
        }
        foreach (var (key, value) in newAssorts)
        {
            trader.QuestAssort[key] = value;
        }
    }

    public void AddTraderProductions(string traderId, List<HideoutProduction> newProductions)
    {
        if (hideoutTable.Production.Recipes is null)
        {
            logger.Warning("Unable to add productions from trader: " + traderId + ", they couldn't be found on the server");
            return;
        }
        hideoutTable.Production.Recipes.AddRange(newProductions);
    }
}

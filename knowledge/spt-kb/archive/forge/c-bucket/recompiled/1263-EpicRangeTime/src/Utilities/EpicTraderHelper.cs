// 反编译复刻 + 4.1 适配：EpicTraderHelper（4.0.13 → 4.1.2）
// 关键修改：databaseService.GetTables() → 注入 TradersTable/LocaleTable（4.1 表模型）
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;

namespace EpicsAIO.Utilities;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class EpicTraderHelper(
    ISptLogger<EpicTraderHelper> logger,
    ICloner cloner,
    TradersTable tradersTable,
    LocaleTable localeTable)
{
    public void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
    {
        var item = new UpdateTime
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
            Base = cloner.Clone<TraderBase>(traderDetailsToAdd)!,
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
            {
                { "Started", new Dictionary<MongoId, MongoId>() },
                { "Success", new Dictionary<MongoId, MongoId>() },
                { "Fail", new Dictionary<MongoId, MongoId>() }
            },
            Dialogue = new Dictionary<string, List<string>?>()
        };
        ((Dictionary<MongoId, Trader>)(object)tradersTable).TryAdd(traderDetailsToAdd.Id, trader);
    }

    public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
    {
        var global = localeTable.Global;
        MongoId newTraderId = baseJson.Id;
        string fullName = baseJson.Name!;
        string nickName = baseJson.Nickname!;
        string location = baseJson.Location!;
        foreach (var (_, lazyLocale) in global)
        {
            lazyLocale.AddTransformer(dict =>
            {
                dict.Add($"{newTraderId} FullName", fullName);
                dict.Add($"{newTraderId} FirstName", firstName);
                dict.Add($"{newTraderId} Nickname", nickName);
                dict.Add($"{newTraderId} Location", location);
                dict.Add($"{newTraderId} Description", description);
                return dict;
            });
        }
    }

    public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
    {
        if (!((Dictionary<MongoId, Trader>)(object)tradersTable).TryGetValue(new MongoId(traderId), out var value))
        {
            logger.Warning("Unable to update assorts for trader: " + traderId + ", they couldn't be found on the server", null);
        }
        else
        {
            value.Assort = newAssorts;
        }
    }
}

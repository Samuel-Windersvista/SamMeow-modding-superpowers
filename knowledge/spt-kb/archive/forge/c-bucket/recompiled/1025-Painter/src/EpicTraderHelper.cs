// 反编译复刻 + 4.1 适配：EpicTraderHelper（4.0.4 → 4.1.2）
// 关键修改：databaseService.GetTables() → 注入 TradersTable/LocaleTable（4.1 表模型）
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;

namespace Painter;

[Injectable(InjectionType.Transient, 2147483647)]
public class EpicTraderHelper(
    ISptLogger<EpicTraderHelper> logger,
    ICloner cloner,
    TradersTable tradersTable,
    LocaleTable localeTable)
{
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
        foreach (var (lang, lazyLocale) in global)
        {
            lazyLocale.AddTransformer(dict =>
            {
                dict[$"{newTraderId} FullName"] = baseJson.Name;
                dict[$"{newTraderId} FirstName"] = firstName;
                dict[$"{newTraderId} Nickname"] = baseJson.Nickname;
                dict[$"{newTraderId} Location"] = baseJson.Location;
                dict[$"{newTraderId} Description"] = description;
                return dict;
            });
        }
    }

    public void OverwriteTraderAssort(MongoId traderId, TraderAssort newAssort)
    {
        if (((Dictionary<MongoId, Trader>)(object)tradersTable).TryGetValue(traderId, out var trader))
        {
            trader.Assort = newAssort;
        }
    }
}

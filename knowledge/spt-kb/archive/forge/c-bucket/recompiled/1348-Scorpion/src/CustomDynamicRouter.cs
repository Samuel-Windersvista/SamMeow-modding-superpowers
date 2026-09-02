// CustomDynamicRouter（反编译重写：4.0 → 4.1.2）
// 关键修改：RouteAction<T> 泛型路由 → 4.1 非泛型 record RouteAction（action 带 CancellationToken，返回 ValueTask<object>）
// DatabaseService.GetTrader → TradersTable.GetTrader（表模型注入）
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators.Ragfair;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _scorpion;

[Injectable(InjectionType.Scoped, 400002)]
public class CustomDynamicRouter : DynamicRouter
{
    private static TraderCallbacks _traderCallbacks = null!;

    private static TradersTable _tradersTable = null!;

    private static RagfairOfferGenerator _ragfairOfferGenerator = null!;

    private static RandomUtil _randomUtil = null!;

    private static ModConfig _modConfig = null!;

    public CustomDynamicRouter(JsonUtil jsonUtil, TraderCallbacks traderCallbacks, TradersTable tradersTable, RagfairOfferGenerator ragfairOfferGenerator, RandomUtil randomUtil)
        : base(jsonUtil, GetCustomRoutes())
    {
        _traderCallbacks = traderCallbacks;
        _tradersTable = tradersTable;
        _ragfairOfferGenerator = ragfairOfferGenerator;
        _randomUtil = randomUtil;
    }

    public void PassConfig(ModConfig config)
    {
        _modConfig = config;
    }

    private static List<RouteAction> GetCustomRoutes()
    {
        List<RouteAction> routes = new(1)
        {
            new RouteAction(
                "/client/trading/api/getTraderAssort/6688d464bc40c867f60e7d7e",
                async (url, info, sessionId, output, cancellationToken) =>
                {
                    List<Item> items = _tradersTable.GetTrader("6688d464bc40c867f60e7d7e")!.Assort.Items;
                    bool addTraderToFlea = _modConfig.AddTraderToFlea;
                    if (_modConfig.RandomizeBuyRestriction)
                    {
                        RandomizeBuyRestriction(items);
                    }
                    if (_modConfig.RandomizeStockAvailable)
                    {
                        RandomizeStockAvailable(items);
                    }
                    if (addTraderToFlea)
                    {
                        _ragfairOfferGenerator.GenerateFleaOffersForTrader("6688d464bc40c867f60e7d7e");
                    }
                    return await _traderCallbacks.GetAssort(url, (EmptyRequestData)info, sessionId);
                })
        };
        return routes;
    }

    private static void RandomizeBuyRestriction(List<Item> assortItems)
    {
        foreach (Item assortItem in assortItems)
        {
            int value = _randomUtil.RandInt(1, 10);
            assortItem.Upd!.BuyRestrictionMax = value;
        }
    }

    private static void RandomizeStockAvailable(List<Item> assortItems)
    {
        foreach (Item assortItem in assortItems)
        {
            assortItem.Upd!.UnlimitedCount = false;
            assortItem.Upd!.StackObjectsCount = 25.0;
            if (_randomUtil.GetChance100((double?)_modConfig.OutOfStockChance))
            {
                assortItem.Upd!.StackObjectsCount = 0.0;
                continue;
            }
            int num = _randomUtil.RandInt(1, 25);
            assortItem.Upd!.StackObjectsCount = num;
        }
    }
}

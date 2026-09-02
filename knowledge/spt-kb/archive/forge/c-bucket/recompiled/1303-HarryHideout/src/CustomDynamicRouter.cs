using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators.Ragfair;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace _harryHideout;

// 4.1.2：DatabaseService -> TradersTable 注入；RouteAction action 为 5 参签名（带 CancellationToken）
[Injectable(InjectionType.Singleton)]
public class CustomDynamicRouter : DynamicRouter
{
	private static TraderCallbacks _traderCallbacks = null!;

	private static ISptLogger<CustomDynamicRouter> _logger = null!;

	private static TradersTable _tradersTable = null!;

	private static RagfairOfferGenerator _ragfairOfferGenerator = null!;

	private static RandomUtil _randomUtil = null!;

	private static ModConfig _modConfig = null!;

	public CustomDynamicRouter(JsonUtil jsonUtil, TraderCallbacks traderCallbacks, ISptLogger<CustomDynamicRouter> logger, TradersTable tradersTable, RagfairOfferGenerator ragfairOfferGenerator, RandomUtil randomUtil)
		: base(jsonUtil, GetCustomRoutes())
	{
		_traderCallbacks = traderCallbacks;
		_logger = logger;
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
		return
		[
			new RouteAction(
				"/client/trading/api/getTraderAssort/67419e9d0d4541ce671543bb",
				async (url, info, sessionId, output, cancellationToken) =>
				{
					List<Item> traderAssortItems = _tradersTable.GetTrader("67419e9d0d4541ce671543bb")!.Assort.Items;
					bool updateFleaOffers = _modConfig.AddTraderToFlea;
					if (_modConfig.RandomizeBuyRestriction)
					{
						RandomizeBuyRestriction(traderAssortItems);
					}
					if (_modConfig.RandomizeStockAvailable)
					{
						RandomizeStockAvailable(traderAssortItems);
					}
					if (updateFleaOffers)
					{
						_ragfairOfferGenerator.GenerateFleaOffersForTrader("67419e9d0d4541ce671543bb");
					}
					// 路由 bodyType 为 null，info 必为 EmptyRequestData
					return await _traderCallbacks.GetAssort(url, (EmptyRequestData)info, sessionId);
				})
		];
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
			assortItem.Upd.StackObjectsCount = 25.0;
			if (_randomUtil.GetChance100(_modConfig.OutOfStockChance))
			{
				assortItem.Upd.StackObjectsCount = 0.0;
				continue;
			}
			int num = _randomUtil.RandInt(1, 25);
			assortItem.Upd.StackObjectsCount = num;
		}
	}
}

using System.Collections.Generic;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;

namespace _harryHideout;

// 4.1.2：DatabaseService 已拆分，商人/语言数据改为表模型注入（TradersTable/LocaleTable）
[Injectable(InjectionType.Singleton)]
public class AddCustomTraderHelper(ISptLogger<AddCustomTraderHelper> logger, ICloner cloner, TradersTable tradersTable, LocaleTable localeTable)
{
	public void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
	{
		UpdateTime item = new UpdateTime
		{
			TraderId = baseJson.Id,
			Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
		};
		traderConfig.UpdateTime.Add(item);
	}

	public void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
	{
		TraderAssort assort = new TraderAssort
		{
			Items = new List<Item>(),
			BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
			LoyalLevelItems = new Dictionary<MongoId, int>()
		};
		// 4.1.2 Trader 为 record：Base/Dialogue/QuestAssort 是 init 属性，必须在对象初始化器中赋值
		Trader trader = new Trader
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
		tradersTable.TryAdd(traderDetailsToAdd.Id, trader);
	}

	public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
	{
		Dictionary<string, LazyLoad<GlobalLocaleDictionary>> global = localeTable.Global;
		MongoId newTraderId = baseJson.Id;
		string fullName = baseJson.Name;
		string nickName = baseJson.Nickname!;
		string location = baseJson.Location!;
		foreach (KeyValuePair<string, LazyLoad<GlobalLocaleDictionary>> kvp in global)
		{
			kvp.Value.AddTransformer(lazyloadedLocaleData =>
			{
				lazyloadedLocaleData!.Add($"{newTraderId} FullName", fullName);
				lazyloadedLocaleData.Add($"{newTraderId} FirstName", firstName);
				lazyloadedLocaleData.Add($"{newTraderId} Nickname", nickName!);
				lazyloadedLocaleData.Add($"{newTraderId} Location", location!);
				lazyloadedLocaleData.Add($"{newTraderId} Description", description);
				return lazyloadedLocaleData;
			});
		}
	}

	public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
	{
		if (!tradersTable.TryGetValue(traderId, out Trader? value))
		{
			logger.Warning("Unable to update assorts for trader: " + traderId + ", they couldn't be found on the server", null);
		}
		else
		{
			value!.Assort = newAssorts;
		}
	}
}

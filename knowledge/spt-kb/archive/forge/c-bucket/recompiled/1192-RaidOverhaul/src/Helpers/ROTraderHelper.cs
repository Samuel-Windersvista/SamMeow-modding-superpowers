// ROTraderHelper: SPT 4.0 -> 4.1.2 迁移
// DatabaseService.GetTables().Traders -> TradersTable 注入
// Trader 属性 init-only -> 对象初始化器构造
// LazyLoad 命名空间: SPTarkov.Server.Core.Utils.Json
using System;
using System.Collections.Generic;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROTraderHelper(ISptLogger<ROTraderHelper> logger, ICloner cloner, TradersTable tradersTable, LocaleTable localeTable)
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
		Trader value = new Trader
		{
			Assort = assort,
			Base = cloner.Clone<TraderBase>(traderDetailsToAdd)!,
			QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
			{
				["Started"] = new Dictionary<MongoId, MongoId>(),
				["Success"] = new Dictionary<MongoId, MongoId>(),
				["Fail"] = new Dictionary<MongoId, MongoId>()
			},
			Dialogue = new Dictionary<string, List<string>?>()
		};
		if (!tradersTable.TryAdd(traderDetailsToAdd.Id, value))
		{
			ROLogger.LogWarning<ROTraderHelper>(logger, "Failed to add trader details to database");
		}
	}

	public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
	{
		Dictionary<string, LazyLoad<GlobalLocaleDictionary>> global = localeTable.Global;
		MongoId newTraderId = baseJson.Id;
		string fullName = baseJson.Name ?? string.Empty;
		string nickName = baseJson.Nickname ?? string.Empty;
		string location = baseJson.Location ?? string.Empty;
		foreach (KeyValuePair<string, LazyLoad<GlobalLocaleDictionary>> item in global)
		{
			var (_, value) = item;
			value.AddTransformer(lazyloadedLocaleData =>
			{
				lazyloadedLocaleData.Add($"{newTraderId} FullName", fullName);
				lazyloadedLocaleData.Add($"{newTraderId} FirstName", firstName);
				lazyloadedLocaleData.Add($"{newTraderId} Nickname", nickName);
				lazyloadedLocaleData.Add($"{newTraderId} Location", location);
				lazyloadedLocaleData.Add($"{newTraderId} Description", description);
				return lazyloadedLocaleData;
			});
		}
	}
}

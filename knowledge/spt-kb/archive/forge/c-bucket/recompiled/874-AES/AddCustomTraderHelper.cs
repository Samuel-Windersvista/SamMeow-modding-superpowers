using System.Collections.Generic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;
using SPTarkov.Common.Models.Logging;

namespace AES_Trader;

[Injectable(InjectionType.Scoped, TypePriority = 400001)]
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
		Trader value = new Trader
		{
			Assort = assort,
			Base = cloner.Clone(traderDetailsToAdd),
			QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
			{
				{
					"Started",
					new Dictionary<MongoId, MongoId>()
				},
				{
					"Success",
					new Dictionary<MongoId, MongoId>()
				},
				{
					"Fail",
					new Dictionary<MongoId, MongoId>()
				}
			},
			Dialogue = new Dictionary<string, List<string>>()
		};
		if (!tradersTable.TryAdd(traderDetailsToAdd.Id, value))
		{
			logger.Warning("Failed to add trader to TradersTable: " + traderDetailsToAdd.Id);
		}
	}

	public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
	{
		Dictionary<string, LazyLoad<GlobalLocaleDictionary>> global = localeTable.Global;
		MongoId newTraderId = baseJson.Id;
		string fullName = baseJson.Name;
		string nickName = baseJson.Nickname;
		string location = baseJson.Location;
		foreach (var (text2, lazyLoad2) in global)
		{
			lazyLoad2.AddTransformer(delegate(GlobalLocaleDictionary? lazyloadedLocaleData)
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

	public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
	{
		if (!tradersTable.TryGetValue(traderId, out Trader value))
		{
			logger.Warning("Unable to update assorts for trader: " + traderId + ", they couldn't be found on the server");
		}
		else
		{
			value.Assort = newAssorts;
		}
	}
}

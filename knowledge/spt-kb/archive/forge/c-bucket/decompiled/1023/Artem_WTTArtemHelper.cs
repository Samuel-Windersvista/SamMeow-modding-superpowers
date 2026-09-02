using System;
using System.Collections.Generic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;

namespace Artem;

[Injectable(/*Could not decode attribute arguments.*/)]
public class WTTArtemHelper(ISptLogger<WTTArtemHelper> logger, ICloner cloner, DatabaseService databaseService, LocaleService localeService)
{
	public void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		UpdateTime val = new UpdateTime
		{
			TraderId = baseJson.Id,
			Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
		};
		traderConfig.UpdateTime.Add(val);
	}

	public void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Expected O, but got Unknown
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		TraderAssort assort = new TraderAssort
		{
			Items = new List<Item>(),
			BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
			LoyalLevelItems = new Dictionary<MongoId, int>()
		};
		Trader val = new Trader
		{
			Assort = assort
		};
		val.set_Base(cloner.Clone<TraderBase>(traderDetailsToAdd));
		Dictionary<string, Dictionary<MongoId, MongoId>> obj = new Dictionary<string, Dictionary<MongoId, MongoId>>();
		obj.Add("Started", new Dictionary<MongoId, MongoId>());
		obj.Add("Success", new Dictionary<MongoId, MongoId>());
		obj.Add("Fail", new Dictionary<MongoId, MongoId>());
		val.set_QuestAssort(obj);
		val.set_Dialogue(new Dictionary<string, List<string>>());
		Trader val2 = val;
		if (databaseService.GetTables().Traders.TryAdd(traderDetailsToAdd.Id, val2))
		{
		}
	}

	public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		Dictionary<string, LazyLoad<Dictionary<string, string>>> global = databaseService.GetTables().Locales.Global;
		MongoId newTraderId = baseJson.Id;
		string fullName = baseJson.Name;
		string nickName = baseJson.Nickname;
		string location = baseJson.Location;
		Enumerator<string, LazyLoad<Dictionary<string, string>>> enumerator = global.GetEnumerator();
		try
		{
			string text = default(string);
			LazyLoad<Dictionary<string, string>> val = default(LazyLoad<Dictionary<string, string>>);
			while (enumerator.MoveNext())
			{
				enumerator.Current.Deconstruct(ref text, ref val);
				string text2 = text;
				LazyLoad<Dictionary<string, string>> val2 = val;
				val2.AddTransformer((Func<Dictionary<string, string>, Dictionary<string, string>>)delegate(Dictionary<string, string>? lazyloadedLocaleData)
				{
					//IL_000f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0048: Unknown result type (might be due to invalid IL or missing references)
					//IL_0081: Unknown result type (might be due to invalid IL or missing references)
					//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
					//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
					lazyloadedLocaleData.Add($"{newTraderId} FullName", fullName);
					lazyloadedLocaleData.Add($"{newTraderId} FirstName", firstName);
					lazyloadedLocaleData.Add($"{newTraderId} Nickname", nickName);
					lazyloadedLocaleData.Add($"{newTraderId} Location", location);
					lazyloadedLocaleData.Add($"{newTraderId} Description", description);
					return lazyloadedLocaleData;
				});
			}
		}
		finally
		{
			((global::System.IDisposable)enumerator/*cast due to constrained. prefix*/).Dispose();
		}
	}

	public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Trader val = default(Trader);
		if (!databaseService.GetTables().Traders.TryGetValue(MongoId.op_Implicit(traderId), ref val))
		{
			logger.Warning("Unable to update assorts for trader: " + traderId + ", they couldn't be found on the server", (global::System.Exception)null);
		}
		else
		{
			val.Assort = newAssorts;
		}
	}
}

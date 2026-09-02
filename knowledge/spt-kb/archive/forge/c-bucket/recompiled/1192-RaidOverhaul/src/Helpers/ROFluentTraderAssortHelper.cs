// ROFluentTraderAssortHelper: SPT 4.0 -> 4.1.2 迁移
// DatabaseService.GetTables().Traders -> TradersTable 注入
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROFluentTraderAssortHelper(TradersTable tradersTable, ISptLogger<ROFluentTraderAssortHelper> logger)
{
	private readonly List<Item> _itemsToSell = new List<Item>();

	private readonly Dictionary<string, List<List<BarterScheme>>> _barterScheme = new Dictionary<string, List<List<BarterScheme>>>();

	private readonly Dictionary<string, int> _loyaltyLevel = new Dictionary<string, int>();

	public ROFluentTraderAssortHelper CreateSingleAssortItem(MongoId itemTpl, MongoId? itemId = null)
	{
		Item item = new Item
		{
			Id = itemId ?? new MongoId(),
			Template = itemTpl,
			ParentId = "hideout",
			SlotId = "hideout",
			Upd = new Upd
			{
				UnlimitedCount = false,
				StackObjectsCount = 100.0
			}
		};
		_itemsToSell.Add(item);
		return this;
	}

	public ROFluentTraderAssortHelper AddStackCount(int stackCount)
	{
		_itemsToSell[0].Upd.StackObjectsCount = stackCount;
		return this;
	}

	public ROFluentTraderAssortHelper AddLoyaltyLevel(int level)
	{
		_loyaltyLevel[(string)_itemsToSell[0].Id] = level;
		return this;
	}

	public ROFluentTraderAssortHelper AddBarterCost(MongoId itemTpl, int count)
	{
		MongoId id = _itemsToSell[0].Id;
		if (_barterScheme.Count == 0)
		{
			BarterScheme val = new BarterScheme
			{
				Count = count,
				Template = itemTpl
			};
			Dictionary<string, List<List<BarterScheme>>> barterScheme = _barterScheme;
			string key = (string)id;
			int num = 1;
			List<List<BarterScheme>> list = new List<List<BarterScheme>>(num);
			CollectionsMarshal.SetCount(list, num);
			ref List<BarterScheme> reference = ref CollectionsMarshal.AsSpan(list)[0];
			int num2 = 1;
			List<BarterScheme> list2 = new List<BarterScheme>(num2);
			CollectionsMarshal.SetCount(list2, num2);
			CollectionsMarshal.AsSpan(list2)[0] = val;
			reference = list2;
			barterScheme[key] = list;
		}
		else
		{
			BarterScheme? val2 = _barterScheme[(string)id][0].FirstOrDefault(x => x.Template == itemTpl);
			if (val2 != null)
			{
				val2.Count += count;
			}
			else
			{
				_barterScheme[(string)id][0].Add(new BarterScheme
				{
					Count = count,
					Template = itemTpl
				});
			}
		}
		return this;
	}

	public ROFluentTraderAssortHelper? Export(string traderId)
	{
		Trader? valueOrDefault = tradersTable.GetValueOrDefault((MongoId)traderId);
		MongoId rootItemAddedId = _itemsToSell.FirstOrDefault().Id;
		if (valueOrDefault.Assort.Items.Exists(x => x.Id == rootItemAddedId))
		{
			logger.Error($"Unable to add complex item with item key: {_itemsToSell[0].Id}, key already in use", (Exception?)null);
			_itemsToSell.Clear();
			_barterScheme.Clear();
			_loyaltyLevel.Clear();
			return null;
		}
		valueOrDefault.Assort.Items.AddRange(_itemsToSell);
		valueOrDefault.Assort.BarterScheme[rootItemAddedId] = _barterScheme[(string)rootItemAddedId];
		valueOrDefault.Assort.LoyalLevelItems[rootItemAddedId] = _loyaltyLevel[(string)rootItemAddedId];
		_itemsToSell.Clear();
		_barterScheme.Clear();
		_loyaltyLevel.Clear();
		return this;
	}

	public void CreateSingleItemOffer(string ItemToAdd, int stackCount, int loyaltyLevelToPush, int reqCost, MongoId currencyToUse, MongoId traderToUse)
	{
		CreateSingleAssortItem((MongoId)ItemToAdd).AddBarterCost(currencyToUse, reqCost).AddStackCount(stackCount).AddLoyaltyLevel(loyaltyLevelToPush)
			.Export((string)traderToUse);
	}
}

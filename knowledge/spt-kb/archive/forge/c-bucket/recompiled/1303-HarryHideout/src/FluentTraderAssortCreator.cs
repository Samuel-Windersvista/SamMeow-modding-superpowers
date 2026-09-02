using System;
using System.Collections.Generic;
using System.Linq;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace _harryHideout;

// 4.1.2锛欴atabaseService -> TradersTable 娉ㄥ叆锛涙竻鐞嗗弽缂栬瘧浜х敓鐨?CollectionsMarshal 涓棿浠ｇ爜
[Injectable(InjectionType.Singleton)]
public class FluentTraderAssortCreator(TradersTable tradersTable, ISptLogger<FluentTraderAssortCreator> logger)
{
	private readonly List<Item> _itemsToSell = new List<Item>();

	private readonly Dictionary<string, List<List<BarterScheme>>> _barterScheme = new Dictionary<string, List<List<BarterScheme>>>();

	private readonly Dictionary<string, int> _loyaltyLevel = new Dictionary<string, int>();

	public FluentTraderAssortCreator CreateSingleAssortItem(MongoId itemTpl, MongoId? itemId = null)
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
				StackObjectsCount = 100.0,
				SpawnedInSession = true
			}
		};
		_itemsToSell.Add(item);
		return this;
	}

	public FluentTraderAssortCreator CreateComplexAssortItem(List<Item> items)
	{
		items[0].ParentId = "hideout";
		items[0].SlotId = "hideout";
		Item val = items[0];
		if (val.Upd == null)
		{
			val.Upd = new Upd();
		}
		items[0].Upd!.UnlimitedCount = false;
		items[0].Upd!.StackObjectsCount = 100.0;
		_itemsToSell.AddRange(items);
		return this;
	}

	public FluentTraderAssortCreator AddStackCount(int stackCount)
	{
		_itemsToSell[0].Upd!.StackObjectsCount = stackCount;
		return this;
	}

	public FluentTraderAssortCreator AddUnlimitedStackCount()
	{
		_itemsToSell[0].Upd!.StackObjectsCount = 999999.0;
		_itemsToSell[0].Upd!.UnlimitedCount = true;
		return this;
	}

	public FluentTraderAssortCreator MakeStackCountUnlimited()
	{
		_itemsToSell[0].Upd!.StackObjectsCount = 999999.0;
		return this;
	}

	public FluentTraderAssortCreator AddBuyRestriction(int maxBuyLimit)
	{
		_itemsToSell[0].Upd!.BuyRestrictionMax = maxBuyLimit;
		_itemsToSell[0].Upd!.BuyRestrictionCurrent = 0;
		return this;
	}

	public FluentTraderAssortCreator AddLoyaltyLevel(int level)
	{
		_loyaltyLevel[_itemsToSell[0].Id] = level;
		return this;
	}

	public FluentTraderAssortCreator AddMoneyCost(string currencyType, int amount)
	{
		BarterScheme barterScheme = new BarterScheme
		{
			Count = amount,
			Template = currencyType
		};
		List<List<BarterScheme>> scheme = new List<List<BarterScheme>>
		{
			new List<BarterScheme> { barterScheme }
		};
		if (!_barterScheme.TryAdd(_itemsToSell[0].Id, scheme))
		{
			logger.Warning("Unable to add barter scheme currency: " + currencyType, null);
		}
		return this;
	}

	public FluentTraderAssortCreator AddBarterCost(MongoId itemTpl, int count)
	{
		MongoId id = _itemsToSell[0].Id;
		if (_barterScheme.Count == 0)
		{
			List<List<BarterScheme>> scheme = new List<List<BarterScheme>>
			{
				new List<BarterScheme>
				{
					new BarterScheme
					{
						Count = count,
						Template = itemTpl
					}
				}
			};
			_barterScheme[id] = scheme;
		}
		else
		{
			BarterScheme? existing = _barterScheme[id][0].FirstOrDefault(x => x.Template == itemTpl);
			if (existing != null)
			{
				existing.Count += count;
			}
			else
			{
				_barterScheme[id][0].Add(new BarterScheme
				{
					Count = count,
					Template = itemTpl
				});
			}
		}
		return this;
	}

	public FluentTraderAssortCreator? Export(string traderId)
	{
		Trader valueOrDefault = tradersTable.GetValueOrDefault(traderId)!;
		MongoId rootItemAddedId = _itemsToSell[0].Id;
		if (valueOrDefault.Assort.Items.Exists(x => x.Id == rootItemAddedId))
		{
			logger.Error($"Unable to add complex item with item key: {_itemsToSell[0].Id}, key already in use", null);
			_itemsToSell.Clear();
			_barterScheme.Clear();
			_loyaltyLevel.Clear();
			return null;
		}
		valueOrDefault.Assort.Items.AddRange(_itemsToSell);
		valueOrDefault.Assort.BarterScheme[rootItemAddedId] = _barterScheme[rootItemAddedId];
		valueOrDefault.Assort.LoyalLevelItems[rootItemAddedId] = _loyaltyLevel[rootItemAddedId];
		_itemsToSell.Clear();
		_barterScheme.Clear();
		_loyaltyLevel.Clear();
		return this;
	}
}

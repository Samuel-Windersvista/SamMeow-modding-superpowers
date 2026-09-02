// FluentTraderAssortCreator（4.0 → 4.1.2 重写）
// 4.1.2 变更：
//   1. DatabaseService.GetTables().Traders → 注入 TradersTable（Dictionary<MongoId, Trader>）
//   2. 内部字典键 string → MongoId（对齐 TraderAssort.BarterScheme/LoyalLevelItems）
//   3. MongoId.op_Implicit 反编译噪声 → 隐式转换
//   4. GetTrader() 返回 Trader?，增加 null 防御
using System;
using System.Collections.Generic;
using System.Linq;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace HoodsEnergyDrinks_CSharp;

[Injectable]
public class FluentTraderAssortCreator(TradersTable tradersTable, ISptLogger<HoodsEnergyDrinks> logger)
{
    private readonly List<Item> _itemsToSell = [];

    private readonly Dictionary<MongoId, List<List<BarterScheme>>> _barterScheme = new();

    private readonly Dictionary<MongoId, int> _loyaltyLevel = new();

    public FluentTraderAssortCreator CreateSingleAssortItem(MongoId itemTpl, MongoId? itemId = null)
    {
        Item item = new()
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

    public FluentTraderAssortCreator CreateComplexAssortItem(List<Item> items)
    {
        items[0].ParentId = "hideout";
        items[0].SlotId = "hideout";
        Upd upd = items[0].Upd ?? new Upd();
        items[0].Upd = upd;
        upd.UnlimitedCount = false;
        upd.StackObjectsCount = 100.0;
        _itemsToSell.AddRange(items);
        return this;
    }

    public FluentTraderAssortCreator AddStackCount(int stackCount)
    {
        Upd upd = _itemsToSell[0].Upd ??= new Upd();
        upd.StackObjectsCount = stackCount;
        return this;
    }

    public FluentTraderAssortCreator AddUnlimitedStackCount()
    {
        Upd upd = _itemsToSell[0].Upd ??= new Upd();
        upd.StackObjectsCount = 999999.0;
        upd.UnlimitedCount = true;
        return this;
    }

    public FluentTraderAssortCreator MakeStackCountUnlimited()
    {
        Upd upd = _itemsToSell[0].Upd ??= new Upd();
        upd.StackObjectsCount = 999999.0;
        return this;
    }

    public FluentTraderAssortCreator AddBuyRestriction(int maxBuyLimit)
    {
        Upd upd = _itemsToSell[0].Upd ??= new Upd();
        upd.BuyRestrictionMax = maxBuyLimit;
        upd.BuyRestrictionCurrent = 0;
        return this;
    }

    public FluentTraderAssortCreator AddLoyaltyLevel(int level)
    {
        _loyaltyLevel[_itemsToSell[0].Id] = level;
        return this;
    }

    public FluentTraderAssortCreator AddMoneyCost(string currencyType, int amount)
    {
        BarterScheme scheme = new()
        {
            Count = amount,
            Template = currencyType
        };

        if (!_barterScheme.TryAdd(_itemsToSell[0].Id, [[scheme]]))
        {
            logger.Warning("Unable to add barter scheme currency: " + currencyType);
        }

        return this;
    }

    public FluentTraderAssortCreator AddBarterCost(MongoId itemTpl, int count)
    {
        MongoId itemId = _itemsToSell[0].Id;
        if (_barterScheme.Count == 0)
        {
            _barterScheme[itemId] = [[new BarterScheme { Count = count, Template = itemTpl }]];
        }
        else
        {
            BarterScheme? existing = _barterScheme[itemId][0].FirstOrDefault(x => x.Template == itemTpl);
            if (existing != null)
            {
                existing.Count += count;
            }
            else
            {
                _barterScheme[itemId][0].Add(new BarterScheme { Count = count, Template = itemTpl });
            }
        }

        return this;
    }

    public FluentTraderAssortCreator? Export(string traderId)
    {
        Trader? trader = tradersTable.GetTrader(traderId);
        if (trader == null)
        {
            logger.Error($"Unable to find trader with id: {traderId}");
            return null;
        }

        MongoId rootItemAddedId = _itemsToSell[0].Id;
        if (trader.Assort.Items.Exists(x => x.Id == rootItemAddedId))
        {
            logger.Error($"Unable to add complex item with item key: {_itemsToSell[0].Id}, key already in use");
            Reset();
            return null;
        }

        trader.Assort.Items.AddRange(_itemsToSell);
        trader.Assort.BarterScheme[rootItemAddedId] = _barterScheme[rootItemAddedId];
        trader.Assort.LoyalLevelItems[rootItemAddedId] = _loyaltyLevel[rootItemAddedId];
        Reset();
        return this;
    }

    private void Reset()
    {
        _itemsToSell.Clear();
        _barterScheme.Clear();
        _loyaltyLevel.Clear();
    }
}

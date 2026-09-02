// TraderHelper（4.0 → 4.1.2 重写）
// 4.1.2 变更：MongoId.op_Implicit(string) 反编译噪声 → 隐式转换；Money.ROUBLES 已是 MongoId
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Enums;

namespace HoodsEnergyDrinks_CSharp;

internal class TraderHelper(FluentTraderAssortCreator assortCreator, ModConfig config, Drink drinks)
{
    private readonly FluentTraderAssortCreator _assortCreator = assortCreator;

    private readonly ModConfig _config = config;

    private readonly Drink _drinks = drinks;

    public void AddSingleItemsToTrader(string traderId)
    {
        foreach (KeyValuePair<string, DrinkProps> drink in _drinks.Items)
        {
            if (!_config.drinks[drink.Key].sold_by_trader)
            {
                continue;
            }

            _assortCreator.CreateSingleAssortItem(drink.Value._id)
                .AddUnlimitedStackCount()
                .AddBuyRestriction(_config.drinks[drink.Key].trader_stock)
                .AddMoneyCost(Money.ROUBLES, _config.drinks[drink.Key].trader_price)
                .AddLoyaltyLevel(_config.drinks[drink.Key].loyalty_level)
                .Export(traderId);
        }
    }
}

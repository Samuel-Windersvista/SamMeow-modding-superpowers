// HoodsEnergyDrinks 主类（4.0 → 4.1.2 重写）
// 4.1.2 变更：
//   1. IOnLoad.OnLoad() → OnLoadAsync(CancellationToken)
//   2. DatabaseService / DatabaseServer 移除 → 直接注入表模型（TemplateTable/LocationTable/GlobalTable/TradersTable）
//   3. ConfigServer.GetConfig<RagfairConfig>() 移除 → 直接注入 RagfairConfig
//   4. MongoId.op_Implicit(string) 反编译噪声 → 字符串隐式转换
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace HoodsEnergyDrinks_CSharp;

[Injectable]
public class HoodsEnergyDrinks(
    ISptLogger<HoodsEnergyDrinks> logger,
    RagfairConfig ragfairConfig,
    CustomItemService customItemService,
    ModHelper modHelper,
    TemplateTable templateTable,
    LocationTable locationTable,
    GlobalTable globalTable,
    TradersTable tradersTable)
    : IOnLoad
{
    private readonly Dictionary<string, MongoId> lootContainerMap = new()
    {
        { "duffle_bag", "578f87a3245977356274f2cb" },
        { "dead_scav", "5909e4b686f7747f5b744fa4" },
        { "jacket", "578f8778245977358849a9b5" },
        { "ration_supply_crate", "5d6fd13186f77424ad2a8c69" },
        { "ground_cache", "5d6d2b5486f774785c2ba8ea" }
    };

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        string configPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(absolutePathToModFolder, "config"));
        ModConfig config = modHelper.GetJsonDataFromFile<ModConfig>(configPath, "config.jsonc");
        Drink drinks = modHelper.GetJsonDataFromFile<Drink>(absolutePathToModFolder, "EnergyDrinkInfo.json");

        FluentTraderAssortCreator assortCreator = new(tradersTable, logger);
        TraderHelper traderHelper = new(assortCreator, config, drinks);
        ItemCreator itemCreator = new(config, drinks);
        itemCreator.BuildItems(globalTable, customItemService, modHelper);

        traderHelper.AddSingleItemsToTrader("54cb57776803fa99248b456e");

        // 将新饮品加入大厅物品（hotrod 等）的可挂载插槽
        List<TemplateItem> halls =
        [
            templateTable.Items["63dbd45917fff4dee40fe16e"],
            templateTable.Items["65424185a57eea37ed6562e9"],
            templateTable.Items["6542435ea57eea37ed6562f0"]
        ];

        // flea 黑名单
        foreach (KeyValuePair<string, DrinkConfig> drink in config.drinks)
        {
            if (drink.Value.flea_banned)
            {
                ragfairConfig.Dynamic.Blacklist.Custom.Add(drinks.Items[drink.Key]._id);
            }
        }

        // 插槽过滤器加入新饮品 id
        foreach (KeyValuePair<string, DrinkProps> drink in drinks.Items)
        {
            foreach (TemplateItem hall in halls)
            {
                if (hall.Properties?.Slots == null)
                {
                    continue;
                }

                foreach (Slot slot in hall.Properties.Slots)
                {
                    if (slot.Properties?.Filters == null)
                    {
                        continue;
                    }

                    foreach (SlotFilter filter in slot.Properties.Filters)
                    {
                        if (filter.Filter == null)
                        {
                            continue;
                        }

                        if (filter.Filter.Contains(drink.Value._id))
                        {
                            filter.Filter.Add(drink.Value._id);
                        }
                    }
                }
            }
        }

        MongoId hotRodEnergyDrinkId = "5751496424597720a27126da";
        string[] mapKeys = ["bigmap", "factory4_day", "factory4_night", "woods", "rezervbase", "shoreline", "interchange", "tarkovstreets", "lighthouse", "laboratory", "sandbox", "sandbox_high"];

        foreach (string map in mapKeys)
        {
            string mappedKey = locationTable.GetMappedKey(map);
            Location location = locationTable.GetDictionary()[mappedKey];

            location.LooseLoot?.AddTransformer(lazyLoadedLooseLoot =>
            {
                if (lazyLoadedLooseLoot == null)
                {
                    return lazyLoadedLooseLoot;
                }

                foreach (Spawnpoint spawnpoint in lazyLoadedLooseLoot.Spawnpoints ?? [])
                {
                    if (spawnpoint.Template?.Items == null)
                    {
                        continue;
                    }

                    foreach (SptLootItem item in spawnpoint.Template.Items)
                    {
                        if (item.Template == hotRodEnergyDrinkId)
                        {
                            foreach (LooseLootItemDistribution dist in spawnpoint.ItemDistribution ?? [])
                            {
                                if (dist.ComposedKey?.Key == item.ComposedKey)
                                {
                                    double? relativeProbability = dist.RelativeProbability;

                                    foreach (KeyValuePair<string, DrinkProps> drink in drinks.Items)
                                    {
                                        if (!config.drinks[drink.Key].enable)
                                        {
                                            continue;
                                        }

                                        string composedKey = drink.Value._id + "_composedkey";
                                        SptLootItem newItem = new()
                                        {
                                            Id = new MongoId(),
                                            Template = drink.Value._id,
                                            ComposedKey = composedKey
                                        };

                                        List<SptLootItem> items = spawnpoint.Template.Items.ToList();
                                        items.Add(newItem);
                                        spawnpoint.Template.Items = items;

                                        LooseLootItemDistribution newDist = new()
                                        {
                                            ComposedKey = new ComposedKey { Key = composedKey },
                                            RelativeProbability = relativeProbability * config.drinks[drink.Key].loose_loot_multiplier
                                        };

                                        List<LooseLootItemDistribution> distList = spawnpoint.ItemDistribution?.ToList() ?? [];
                                        distList.Add(newDist);
                                        spawnpoint.ItemDistribution = distList;
                                    }
                                }
                            }
                        }
                    }
                }

                return lazyLoadedLooseLoot;
            });

            location.StaticLoot?.AddTransformer(lazyLoadedStaticLoot =>
            {
                if (lazyLoadedStaticLoot == null)
                {
                    return lazyLoadedStaticLoot;
                }

                foreach (KeyValuePair<string, DrinkProps> drink in drinks.Items)
                {
                    if (!config.drinks[drink.Key].enable)
                    {
                        continue;
                    }

                    Dictionary<string, float> weights = itemCreator.Loot.StaticLoot[drink.Value._id].Weights;
                    foreach (KeyValuePair<string, float> weight in weights)
                    {
                        if (!lootContainerMap.TryGetValue(weight.Key, out MongoId containerId))
                        {
                            continue;
                        }

                        float probability = GetProbability(lazyLoadedStaticLoot, weight.Key, hotRodEnergyDrinkId);
                        try
                        {
                            ItemDistribution newDist = new()
                            {
                                Tpl = drink.Value._id,
                                RelativeProbability = weight.Value * probability
                            };

                            List<ItemDistribution> distList = lazyLoadedStaticLoot[containerId].ItemDistribution?.ToList() ?? [];
                            distList.Add(newDist);
                            lazyLoadedStaticLoot[containerId].ItemDistribution = distList;
                        }
                        catch
                        {
                            // 容器分布缺失时忽略
                        }
                    }
                }

                return lazyLoadedStaticLoot;
            });
        }

        logger.Success("[Hoods Energy Drinks] Successfully added to server!");
        return Task.CompletedTask;
    }

    private float GetProbability(Dictionary<MongoId, StaticLootDetails> mapStaticLoot, string lootContainerString, MongoId hotRodId)
    {
        float result = 400f;
        MongoId containerId = lootContainerMap[lootContainerString];

        foreach (KeyValuePair<MongoId, StaticLootDetails> kvp in mapStaticLoot)
        {
            if (kvp.Key != containerId)
            {
                continue;
            }

            foreach (ItemDistribution item in kvp.Value.ItemDistribution ?? [])
            {
                if (item.Tpl == hotRodId)
                {
                    return item.RelativeProbability.GetValueOrDefault();
                }
            }
        }

        return result;
    }
}

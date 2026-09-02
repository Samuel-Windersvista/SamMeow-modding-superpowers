// SecureMapbook 主类（反编译重写，4.0 → 4.1.2）
// 关键修改：
//   1. OnLoad() → OnLoadAsync(CancellationToken)（4.1.2 IOnLoad 签名）
//   2. DatabaseService 已删除 → 表模型注入：TemplateTable（Items）/ TradersTable（GetTrader）/ LocationTable（GetDictionary）
//   3. NewItemFromCloneDetails：NewId/ParentId/ItemTplToClone 改为 MongoId；新增 required NewItemName
//   4. Slot.Id/Parent 改为 MongoId?；MongoId.op_Implicit 改隐式转换
//   5. AddMapbookToMaps：反射 StaticLoot 改为 LocationTable + LazyLoad.AddTransformer（幂等）
//   6. CustomItemService/ModHelper/ISptLogger 命名空间变更
//   7. [Injectable(InjectionType.Scoped, 499999)]（4.1.2 两参数签名）
using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using SPTarkov.Server.Core.Utils.Json;
using securemapbooke;
using securemapbooke.Models;

namespace _securemapbook;

[Injectable(InjectionType.Scoped, 499999)]
public class SecureMapbook(ISptLogger<SecureMapbook> logger, TemplateTable templateTable, TradersTable traderTable, LocationTable locationTable, CustomItemService customItemService, ModHelper modHelper) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        ModConfig jsonDataFromFile = modHelper.GetJsonDataFromFile<ModConfig>(absolutePathToModFolder, "config/config.json");
        jsonDataFromFile.Locales = modHelper.GetJsonDataFromFile<LocalesWrapper>(absolutePathToModFolder, "config/locales.json")?.Locales ?? new Dictionary<string, CustomLocaleDetails>();
        jsonDataFromFile.BarterItems = ItemIds.Barter.Items;
        jsonDataFromFile.OrganizationalPouch = ItemIds.OrganizationalPouches.AllowedPouches;
        CreateMapbookItem(customItemService, jsonDataFromFile);
        AddMapbookToMaps(jsonDataFromFile, locationTable);
        jsonDataFromFile.SpecialSlotsList = GetSpecialSlotIds(templateTable, jsonDataFromFile);
        jsonDataFromFile.SecureContainers = GetSecureContainerIds(templateTable, jsonDataFromFile);
        if (jsonDataFromFile.AllowInSecureContainers)
        {
            AllowInSecureContainers(jsonDataFromFile);
        }
        if (jsonDataFromFile.AllowInOrganizationalPouchs)
        {
            AllowInOrganizationalPouches(jsonDataFromFile);
        }
        if (jsonDataFromFile.AllowInSpecialSlots)
        {
            AllowInSpecialSlots(jsonDataFromFile);
        }
        if (!jsonDataFromFile.AllowInsurance)
        {
            DisableMapsInsurance(jsonDataFromFile);
        }
        logger.Success(ValidateModChanges(jsonDataFromFile, templateTable, traderTable, customItemService));
        return Task.CompletedTask;
    }

    private void CreateMapbookItem(CustomItemService customItemService, ModConfig config)
    {
        Dictionary<string, LocaleDetails> locales = config.Locales.ToDictionary(kvp => kvp.Key, kvp => new LocaleDetails
        {
            Name = kvp.Value.Name,
            ShortName = kvp.Value.ShortName,
            Description = kvp.Value.Description
        });
        NewItemFromCloneDetails val = new NewItemFromCloneDetails
        {
            ItemTplToClone = new MongoId("5f4f9eb969cdc30ff33f09db"),
            NewId = new MongoId("6621a2e3a8d8b1a9f0e3b4c5"),
            NewItemName = "Secure Mapbook",
            ParentId = new MongoId("55818a104bdc2db9688b4569"),
            FleaPriceRoubles = config.Price,
            HandbookPriceRoubles = config.Price,
            HandbookParentId = "5b47574386f77428ca22b345",
            Locales = locales,
            OverrideProperties = GetMapbookProperties(config)
        };
        if (config.EnableDebugging)
        {
            logger.Info("[SecureMapbook] Created Mapbook with ID: 6621a2e3a8d8b1a9f0e3b4c5");
        }
        customItemService.CreateItemFromClone(val);
        AddToTraderAssort(new MongoId("54cb50c76803fa8b248b4571"), new MongoId("6621a2e3a8d8b1a9f0e3b4c5"), config);
    }

    private TemplateItemProperties GetMapbookProperties(ModConfig config)
    {
        return new TemplateItemProperties
        {
            Name = "Secure Mapbook",
            ShortName = "Mapbook",
            Description = "A meticulously crafted book designed for storing and organizing maps...",
            Prefab = new Prefab
            {
                Path = "assets/content/items/barter/item_mapbook/mapbook.bundle"
            },
            Width = config.Size.Width,
            Height = config.Size.Height,
            CanPutIntoDuringTheRaid = true,
            RaidModdable = true,
            InsuranceDisabled = !config.AllowInsurance,
            CanSellOnRagfair = false,
            ItemSound = "item_book",
            Grids = new List<Grid>(),
            Slots = GenerateMapSlots(config),
            ExaminedByDefault = false
        };
    }

    private List<Slot> GenerateMapSlots(ModConfig config)
    {
        List<Slot> list = new List<Slot>();
        FieldInfo[] fields = typeof(ItemIds.Maps).GetFields(BindingFlags.Static | BindingFlags.Public);
        int num = 0;
        foreach (FieldInfo fieldInfo in fields)
        {
            string name = fieldInfo.Name;
            string? text = fieldInfo.GetValue(null) as string;
            if (!string.IsNullOrEmpty(text))
            {
                Slot item = new Slot
                {
                    Name = "mod_mount_" + (num + 1).ToString().PadLeft(2, '0'),
                    Id = $"5d235bb686f77443f433127{(char)(ushort)(98 + num)}",
                    Parent = "55818b224bdc2dde698b456f",
                    Properties = new SlotProperties
                    {
                        Filters = new List<SlotFilter>
                        {
                            new SlotFilter
                            {
                                Filter = new HashSet<MongoId>
                                {
                                    new MongoId(text)
                                }
                            }
                        }
                    },
                    Required = false,
                    MergeSlotWithChildren = false,
                    Prototype = "55d4af244bdc2d962f8b4571"
                };
                list.Add(item);
                if (config.EnableDebugging)
                {
                    logger.Info($"[SecureMapbook] Added slot for {name} ({text})");
                }
                num++;
            }
        }
        return list;
    }

    private void AddToTraderAssort(MongoId traderId, MongoId itemId, ModConfig config)
    {
        Trader? trader = traderTable.GetTrader(traderId);
        if (trader is null)
        {
            logger.Error($"[SecureMapbook] Failed to add Mapbook to trader assort: trader {traderId} not found");
            return;
        }
        TraderAssort assort = trader.Assort;
        if (ItemIds.Barter.Items.Count > 0)
        {
            MongoId val = new MongoId();
            Item item = new Item
            {
                Id = val,
                Template = itemId,
                ParentId = "hideout",
                SlotId = "hideout",
                Upd = new Upd
                {
                    UnlimitedCount = false,
                    StackObjectsCount = 1.0,
                    BuyRestrictionMax = 50,
                    BuyRestrictionCurrent = 0
                }
            };
            assort.Items.Add(item);
            List<BarterScheme> list = new List<BarterScheme>();
            foreach (BarterItemConfig barterItem in config.BarterItems)
            {
                list.Add(new BarterScheme
                {
                    Template = barterItem.ItemId,
                    Count = barterItem.Count
                });
            }
            assort.BarterScheme[val] = new List<List<BarterScheme>> { list };
            assort.LoyalLevelItems[val] = config.LoyaltyLevelBarter;
            if (config.EnableDebugging)
            {
                logger.Info($"[SecureMapbook] Added barter version (Lvl {config.LoyaltyLevelBarter}) of {itemId} to trader {traderId}");
            }
        }
        MongoId val2 = new MongoId();
        Item item2 = new Item
        {
            Id = val2,
            Template = itemId,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = false,
                StackObjectsCount = 1.0,
                BuyRestrictionMax = 50,
                BuyRestrictionCurrent = 0
            }
        };
        assort.Items.Add(item2);
        List<BarterScheme> item3 = new List<BarterScheme>
        {
            new BarterScheme
            {
                Template = ItemTpl.MONEY_ROUBLES,
                Count = config.Price
            }
        };
        assort.BarterScheme[val2] = new List<List<BarterScheme>> { item3 };
        assort.LoyalLevelItems[val2] = config.LoyaltyLevelBuy;
        if (config.EnableDebugging)
        {
            logger.Info($"[SecureMapbook] Added cash version (Lvl {config.LoyaltyLevelBuy}) of {itemId} to trader {traderId} for {config.Price} roubles");
        }
    }

    private void AddMapbookToMaps(ModConfig config, LocationTable locationTable)
    {
        MongoId mapbookId = new MongoId("6621a2e3a8d8b1a9f0e3b4c5");
        foreach (Location location in locationTable.GetDictionary().Values)
        {
            LazyLoad<Dictionary<MongoId, StaticLootDetails>>? staticLoot = location.StaticLoot;
            if (staticLoot is null)
            {
                continue;
            }
            // transformer 每次 Value 访问都会重新应用到反序列化的新对象图，因此必须幂等
            staticLoot.AddTransformer(dict =>
            {
                if (dict is null)
                {
                    return dict;
                }
                foreach (StaticLootDetails details in dict.Values)
                {
                    if (details.ItemDistribution?.Any(d => d.Tpl == mapbookId) == true)
                    {
                        continue;
                    }
                    ItemDistribution newDistribution = new ItemDistribution
                    {
                        Tpl = mapbookId,
                        RelativeProbability = 1f
                    };
                    details.ItemDistribution = details.ItemDistribution?.Append(newDistribution) ?? new List<ItemDistribution> { newDistribution };
                }
                return dict;
            });
        }
    }

    public Dictionary<string, string> GetSecureContainerIds(TemplateTable templateTable, ModConfig config)
    {
        Dictionary<string, string> dictionary = new Dictionary<string, string>();
        Dictionary<MongoId, TemplateItem> items = templateTable.Items;
        foreach (KeyValuePair<MongoId, TemplateItem> item in items)
        {
            TemplateItem value = item.Value;
            if (value is null || value.Parent.IsEmpty || value.Parent != new MongoId("5448bf274bdc2dfc2f8b456a"))
            {
                continue;
            }
            string value2 = value.Name ?? "Unknown";
            if (!dictionary.ContainsKey(item.Key.ToString()))
            {
                dictionary.Add(item.Key.ToString(), value2);
            }
            if (config.EnableDebugging)
            {
                logger.Info($"[SecureMapbook] Found secure container: {value2} ({item.Key})");
            }
        }
        return dictionary;
    }

    private void AllowInSecureContainers(ModConfig config)
    {
        HashSet<string> hashSet = new HashSet<string> { "5c0a794586f77461c458f892" };
        foreach (string key in config.SecureContainers.Keys)
        {
            if (hashSet.Contains(key))
            {
                if (config.EnableDebugging)
                {
                    logger.Info("[SecureMapbook] Skipping excluded container " + key);
                }
                continue;
            }
            try
            {
                TemplateItem val = templateTable.Items[new MongoId(key)];
                val.Properties ??= new TemplateItemProperties();
                if (val.Properties.Grids is IEnumerable<Grid> grids && grids.Any())
                {
                    Grid val5 = grids.First();
                    val5.Properties ??= new GridProperties();
                    IEnumerable<GridFilter>? filters = val5.Properties.Filters;
                    if (filters is null)
                    {
                        val5.Properties.Filters = new List<GridFilter>
                        {
                            new GridFilter
                            {
                                Filter = new HashSet<MongoId>()
                            }
                        };
                        filters = val5.Properties.Filters;
                    }
                    GridFilter val9 = filters.First();
                    val9.Filter ??= new HashSet<MongoId>();
                    val9.Filter.Add(new MongoId("6621a2e3a8d8b1a9f0e3b4c5"));
                    if (config.EnableDebugging)
                    {
                        logger.Info("[SecureMapbook] Added Mapbook to secure container " + key);
                    }
                }
            }
            catch (Exception value)
            {
                logger.Error($"[SecureMapbook] Failed to add Mapbook to secure container {key}: {value}");
            }
        }
    }

    private void AllowInOrganizationalPouches(ModConfig config)
    {
        foreach (KeyValuePair<string, string> item in config.OrganizationalPouch)
        {
            string value = item.Value;
            string key = item.Key;
            try
            {
                TemplateItem val = templateTable.Items[new MongoId(value)];
                val.Properties ??= new TemplateItemProperties();
                if (val.Properties.Grids is IEnumerable<Grid> grids && grids.Any())
                {
                    Grid val5 = grids.First();
                    val5.Properties ??= new GridProperties();
                    IEnumerable<GridFilter>? filters = val5.Properties.Filters;
                    if (filters is null)
                    {
                        val5.Properties.Filters = new List<GridFilter>
                        {
                            new GridFilter
                            {
                                Filter = new HashSet<MongoId>()
                            }
                        };
                        filters = val5.Properties.Filters;
                    }
                    GridFilter val9 = filters.First();
                    val9.Filter ??= new HashSet<MongoId>();
                    val9.Filter.Add(new MongoId("6621a2e3a8d8b1a9f0e3b4c5"));
                    if (config.EnableDebugging)
                    {
                        logger.Info("[SecureMapbook] Added Mapbook to organizational pouch " + key + " : " + value);
                    }
                }
            }
            catch (Exception value2)
            {
                logger.Error($"[SecureMapbook] Failed to add Mapbook to organizational pouch {key} :  {value} : {value2}");
            }
        }
    }

    public List<string> GetSpecialSlotIds(TemplateTable templateTable, ModConfig config)
    {
        List<string> list = new List<string>();
        Dictionary<MongoId, TemplateItem> items = templateTable.Items;
        foreach (KeyValuePair<MongoId, TemplateItem> item in items)
        {
            TemplateItem value = item.Value;
            if (value?.Properties?.Slots is null)
            {
                continue;
            }
            foreach (Slot slot in value.Properties.Slots)
            {
                if (slot is not null && slot.Id is not null && slot.Name?.Contains("SpecialSlot", StringComparison.OrdinalIgnoreCase) == true)
                {
                    list.Add(slot.Id.Value.ToString());
                    if (config.EnableDebugging)
                    {
                        logger.Info($"[SecureMapbook] Found Special Slot, {slot.Name} ({slot.Id})");
                    }
                }
            }
        }
        return list.Distinct().ToList();
    }

    private void AllowInSpecialSlots(ModConfig config)
    {
        Dictionary<MongoId, TemplateItem> items = templateTable.Items;
        foreach (KeyValuePair<MongoId, TemplateItem> item in items)
        {
            TemplateItem value = item.Value;
            if (value?.Properties?.Slots is null)
            {
                continue;
            }
            foreach (Slot slot in value.Properties.Slots)
            {
                if (slot.Id is not null && config.SpecialSlotsList.Contains(slot.Id.Value.ToString()))
                {
                    slot.Properties ??= new SlotProperties();
                    List<SlotFilter> list = slot.Properties.Filters?.ToList() ?? new List<SlotFilter>();
                    if (!list.Any())
                    {
                        list.Add(new SlotFilter
                        {
                            Filter = new HashSet<MongoId>()
                        });
                    }
                    list[0].Filter ??= new HashSet<MongoId>();
                    list[0].Filter!.Add(new MongoId("6621a2e3a8d8b1a9f0e3b4c5"));
                    slot.Properties.Filters = list;
                    if (config.EnableDebugging)
                    {
                        logger.Info($"[SecureMapbook] Added Mapbook to special slot in container {item.Key}");
                    }
                }
            }
        }
    }

    private void DisableMapsInsurance(ModConfig config)
    {
        try
        {
            TemplateItem val = templateTable.Items[new MongoId("6621a2e3a8d8b1a9f0e3b4c5")];
            val.Properties ??= new TemplateItemProperties();
            val.Properties.InsuranceDisabled = true;
            if (config.EnableDebugging)
            {
                logger.Info("[SecureMapbook] Insurance disabled for Mapbook 6621a2e3a8d8b1a9f0e3b4c5");
            }
            FieldInfo[] fields = typeof(ItemIds.Maps).GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo fieldInfo in fields)
            {
                string name = fieldInfo.Name;
                string? text = fieldInfo.GetValue(null) as string;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }
                try
                {
                    TemplateItem val5 = templateTable.Items[new MongoId(text)];
                    val5.Properties ??= new TemplateItemProperties();
                    val5.Properties.InsuranceDisabled = true;
                    if (config.EnableDebugging)
                    {
                        logger.Info($"[SecureMapbook] Insurance disabled for map '{name}' ({text})");
                    }
                }
                catch (Exception value)
                {
                    logger.Error($"[SecureMapbook] Failed to disable insurance for map '{name}' ({text}): {value}");
                }
            }
        }
        catch (Exception value2)
        {
            logger.Error($"[SecureMapbook] Failed during DisableMapsInsurance process: {value2}");
        }
    }

    private string ValidateModChanges(ModConfig config, TemplateTable templateTable, TradersTable traderTable, CustomItemService customItemService)
    {
        bool flag = true;
        try
        {
            Dictionary<MongoId, TemplateItem> items = templateTable.Items;
            if (!items.ContainsKey(new MongoId("6621a2e3a8d8b1a9f0e3b4c5")))
            {
                logger.Error("[SecureMapbook] Mapbook item 6621a2e3a8d8b1a9f0e3b4c5 not found in item database.");
                flag = false;
            }
            Trader? trader = traderTable.GetTrader(new MongoId("54cb50c76803fa8b248b4571"));
            if (trader is null || !trader.Assort.Items.Any(i => i.Template == new MongoId("6621a2e3a8d8b1a9f0e3b4c5")))
            {
                logger.Error("[SecureMapbook] Trader 54cb50c76803fa8b248b4571 has no assort entry for the Mapbook.");
                flag = false;
            }
        }
        catch (Exception value)
        {
            logger.Error($"[SecureMapbook] Validation failed with exception: {value}");
            flag = false;
        }
        if (!flag)
        {
            return "[SecureMapbook] Some mod config changes failed validation.";
        }
        return "[SecureMapbook] loaded successfully!";
    }
}

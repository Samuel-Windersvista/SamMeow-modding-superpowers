// ROAssortHelper: SPT 4.0 -> 4.1.2 迁移
// DatabaseService -> TemplateTable/TradersTable 注入
// ConfigServer.GetConfig<TraderConfig>() -> 直接注入 TraderConfig
// Preset 命名空间: Eft.Common.Tables -> Spt.Tables
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Path = System.IO.Path;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Items;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Services.Server;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROAssortHelper(
	ISptLogger<ROAssortHelper> logger,
	TemplateTable templateTable,
	TradersTable tradersTable,
	LocaleService localeService,
	HandbookHelper handbookHelper,
	ItemHelper itemHelper,
	PresetHelper presetHelper,
	RandomUtil randomUtil,
	ItemFilterService itemFilterService,
	SeasonalEventService seasonalEventService,
	TraderConfig traderConfig,
	ROHelpers helpers,
	ROFluentTraderAssortHelper fluentAssortHelper,
	ICloner cloner)
{
	protected readonly TraderConfig TraderConfig = traderConfig;

	public void GenerateTraderAssorts(string traderId, DebugFile debugConfig)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		seasonalEventService.GetInactiveSeasonalEventItems();
		Trader? trader = tradersTable.GetTrader((MongoId)traderId);
		TraderAssort? val = trader?.Assort;
		Dictionary<MongoId, Preset>.ValueCollection values = presetHelper.GetDefaultPresets().Values;
		Dictionary<string, string> localeDb = localeService.GetLocaleDb(null);
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		string dataPath = Path.Combine("db", "devFiles");
		ShopInfoFile shopInfoFile = helpers.LoadConfig<ShopInfoFile>(executingAssembly, dataPath, "shopInfo.json");
		foreach (var (val4, val5) in items)
		{
			if (!string.Equals(val5.Type, "Item", StringComparison.OrdinalIgnoreCase) || itemFilterService.IsItemBlacklisted(val4) || (shopInfoFile.ShopBlacklist != null && shopInfoFile.ShopBlacklist.Contains(val4)) || itemFilterService.IsItemRewardBlacklisted(val4) || !itemHelper.IsValidItem(val4, (ISet<MongoId>?)null) || itemHelper.IsOfBaseclass(val4, BaseClasses.BUILT_IN_INSERTS))
			{
				continue;
			}
			if (itemHelper.IsOfBaseclass(val4, BaseClasses.VEST))
			{
				TemplateItemProperties? properties = val5.Properties;
				if (properties?.Slots != null && val5.Properties.Slots.Any())
				{
					continue;
				}
			}
			if ((itemHelper.IsOfBaseclass(val4, BaseClasses.AMMO) || itemHelper.IsOfBaseclass(val4, BaseClasses.AMMO_BOX)) && randomUtil.GetChance100(9.0))
			{
				GenerateAssortItems(val4, val5, val, 50, 300);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
			if (itemHelper.IsOfBaseclass(val4, BaseClasses.ARMOR_PLATE) && randomUtil.GetChance100(13.0))
			{
				GenerateAssortItems(val4, val5, val, 0, 10);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
			if ((itemHelper.IsOfBaseclass(val4, BaseClasses.MEDS) || itemHelper.IsOfBaseclass(val4, BaseClasses.MED_KIT) || itemHelper.IsOfBaseclass(val4, BaseClasses.MEDICAL_SUPPLIES) || itemHelper.IsOfBaseclass(val4, BaseClasses.MEDICAL) || itemHelper.IsOfBaseclass(val4, BaseClasses.STIMULATOR)) && randomUtil.GetChance100(20.0))
			{
				GenerateAssortItems(val4, val5, val, 0, 10);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
			if (itemHelper.IsOfBaseclass(val4, BaseClasses.EQUIPMENT) && !itemHelper.IsOfBaseclass(val4, BaseClasses.ARMORED_EQUIPMENT) && randomUtil.GetChance100(13.0))
			{
				GenerateAssortItems(val4, val5, val, 0, 10);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
			if (itemHelper.IsOfBaseclass(val4, BaseClasses.MOD) && randomUtil.GetChance100(5.0))
			{
				GenerateAssortItems(val4, val5, val, 0, 10);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
			if (itemHelper.IsOfBaseclass(val4, BaseClasses.WEAPON) && randomUtil.GetChance100(7.0))
			{
				GenerateAssortItems(val4, val5, val, 0, 3);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
			if ((itemHelper.IsOfBaseclass(val4, BaseClasses.BUILDING_MATERIAL) || itemHelper.IsOfBaseclass(val4, BaseClasses.BARTER_ITEM) || itemHelper.IsOfBaseclass(val4, BaseClasses.FOOD) || itemHelper.IsOfBaseclass(val4, BaseClasses.DRINK)) && randomUtil.GetChance100(11.0))
			{
				GenerateAssortItems(val4, val5, val, 0, 10);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
			if (shopInfoFile.SpecialShopItems != null && shopInfoFile.SpecialShopItems.Contains(val4) && randomUtil.GetChance100(9.0))
			{
				GenerateAssortItems(val4, val5, val, 0, 1);
				if (debugConfig.DebugMode)
				{
					ROLogger.Log<ROAssortHelper>(logger, "Finished adding item " + localeDb[(string)val4 + " Name"] + " to the Req shop", LogTextColor.Cyan);
				}
			}
		}
		foreach (Preset item in values)
		{
			if (!randomUtil.GetChance100(7.0))
			{
				continue;
			}
			List<Item>? cloned = cloner.Clone<List<Item>>(item.Items);
			IEnumerable<Item> enumerable = ItemExtensions.ReplaceIDs(cloned!);
			Item? obj = enumerable.FirstOrDefault(item2 => string.IsNullOrEmpty(item2.ParentId));
			obj.ParentId = "hideout";
			obj.SlotId = "hideout";
			obj.Upd = new Upd
			{
				StackObjectsCount = helpers.GenRandomCount(1, 3),
				SptPresetId = item.Id
			};
			val.Items.AddRange(enumerable);
			double templatePriceForItems = handbookHelper.GetTemplatePriceForItems(enumerable);
			double itemQualityModifierForItems = itemHelper.GetItemQualityModifierForItems(enumerable, false);
			double cost = templatePriceForItems * itemQualityModifierForItems;
			double finalItemCost = GetFinalItemCost(cost);
			MongoId moneyType = GetMoneyType(cost);
			Dictionary<MongoId, List<List<BarterScheme>>> barterScheme = val.BarterScheme;
			MongoId id = enumerable.First().Id;
			List<List<BarterScheme>> list = new List<List<BarterScheme>>(1);
			CollectionsMarshal.SetCount(list, 1);
			CollectionsMarshal.AsSpan(list)[0] = new List<BarterScheme>
			{
				new BarterScheme
				{
					Template = moneyType,
					Count = finalItemCost
				}
			};
			barterScheme[id] = list;
			val.LoyalLevelItems[enumerable.First().Id] = helpers.GenRandomCount(1, 4);
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<ROAssortHelper>(logger, "Finished adding preset " + item.Name + " to the Req shop", LogTextColor.Cyan);
			}
		}
	}

	public void AddCustomItemsToTraderShop(string traderId, DebugFile debugConfig)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string dataPath = Path.Combine("db", "devFiles");
		Trader? trader = tradersTable.GetTrader((MongoId)traderId);
		TraderAssort? val = trader?.Assort;
		Dictionary<MongoId, Preset>.ValueCollection values = helpers.LoadConfig<Dictionary<MongoId, Preset>>(executingAssembly, dataPath, "customPresets.json").Values;
		string text = helpers.FetchIdFromMap("ReqCoins", ClassMaps.CustomItemMap);
		string text2 = helpers.FetchIdFromMap("ReqSlips", ClassMaps.CustomItemMap);
		string text3 = helpers.FetchIdFromMap("SpecialSlips", ClassMaps.CustomItemMap);
		string text4 = helpers.FetchIdFromMap("MONEY_RUB", ClassMaps.AllItemList);
		fluentAssortHelper.CreateSingleItemOffer("66280a30d3b6f288cb6b9653", randomUtil.RandInt(50, 300), 1, (int)GetCoinCost(helpers.GetItemInHandbook("66280a30d3b6f288cb6b9653").Price!.Value), (MongoId)text, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("662809f445b5ff428e21ac0a", randomUtil.RandInt(50, 300), 1, (int)GetCoinCost(helpers.GetItemInHandbook("662809f445b5ff428e21ac0a").Price!.Value), (MongoId)text, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("662808ec26a8e83120bb25fe", randomUtil.RandInt(50, 300), 1, (int)GetCoinCost(helpers.GetItemInHandbook("662808ec26a8e83120bb25fe").Price!.Value), (MongoId)text, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("6628185208dd86f969db7e03", randomUtil.RandInt(50, 300), 1, (int)GetCoinCost(helpers.GetItemInHandbook("6628185208dd86f969db7e03").Price!.Value), (MongoId)text, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("662818a23a552da6aef8fada", randomUtil.RandInt(50, 300), 1, (int)GetCoinCost(helpers.GetItemInHandbook("662818a23a552da6aef8fada").Price!.Value), (MongoId)text, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("66281ab7fca966e5021f81b5", randomUtil.RandInt(10, 50), 1, (int)GetCoinCost(helpers.GetItemInHandbook("66281ab7fca966e5021f81b5").Price!.Value), (MongoId)text, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("66281ac038f9aebf6f914138", randomUtil.RandInt(5, 30), 1, (int)GetCoinCost(helpers.GetItemInHandbook("66281ac038f9aebf6f914138").Price!.Value), (MongoId)text, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("67c957ce411e6263333a1c38", 1, 4, 1, (MongoId)text3, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("666361eff60f4ea5a464eb70", 1, 4, 3, (MongoId)text3, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("664a55d84a90fc2c8a6305c9", 1, 1, 1, (MongoId)text3, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("67222453e6aee984bcfcf9d1", 1, 2, 5, (MongoId)text2, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("6722254fd847a7aafccfbb54", 1, 1, 10, (MongoId)text2, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("6722252e82ca09a7e62c4d84", 1, 3, 20, (MongoId)text2, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("67d4373526a3cfb1ff5338bb", 1, 2, 15, (MongoId)text2, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("67fefea22cc2bce48d31e21f", 1, 2, 5, (MongoId)text2, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer("67fefe885041d8c121e93bf8", 1, 2, 20, (MongoId)text2, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer(text3, helpers.GenRandomCount(100, 7000), 1, 50, (MongoId)text2, (MongoId)traderId);
		fluentAssortHelper.CreateSingleItemOffer(text, helpers.GenRandomCount(100, 7000), 1, helpers.GenRandomCount(131, 218), (MongoId)text4, (MongoId)traderId);
		double num = Math.Round(308.5657142857143);
		fluentAssortHelper.CreateSingleItemOffer(text2, helpers.GenRandomCount(1, 20), 1, helpers.GenRandomCount((int)(num * 0.75), (int)(num * 1.25)), (MongoId)text, (MongoId)traderId);
		foreach (Preset item in values)
		{
			if (!randomUtil.GetChance100(15.0))
			{
				continue;
			}
			List<Item>? cloned = cloner.Clone<List<Item>>(item.Items);
			IEnumerable<Item> enumerable = ItemExtensions.ReplaceIDs(cloned!);
			Item? obj = enumerable.FirstOrDefault(item2 => string.IsNullOrEmpty(item2.ParentId));
			obj.ParentId = "hideout";
			obj.SlotId = "hideout";
			obj.Upd = new Upd
			{
				StackObjectsCount = 1.0,
				SptPresetId = item.Id
			};
			val.Items.AddRange(enumerable);
			double templatePriceForItems = handbookHelper.GetTemplatePriceForItems(enumerable);
			double itemQualityModifierForItems = itemHelper.GetItemQualityModifierForItems(enumerable, false);
			double cost = templatePriceForItems * itemQualityModifierForItems;
			double finalItemCost = GetFinalItemCost(cost);
			MongoId moneyType = GetMoneyType(cost);
			Dictionary<MongoId, List<List<BarterScheme>>> barterScheme = val.BarterScheme;
			MongoId id = enumerable.First().Id;
			List<List<BarterScheme>> list = new List<List<BarterScheme>>(1);
			CollectionsMarshal.SetCount(list, 1);
			CollectionsMarshal.AsSpan(list)[0] = new List<BarterScheme>
			{
				new BarterScheme
				{
					Template = moneyType,
					Count = finalItemCost
				}
			};
			barterScheme[id] = list;
			val.LoyalLevelItems[enumerable.First().Id] = helpers.GenRandomCount(1, 4);
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<ROAssortHelper>(logger, "Finished adding preset " + item.Name + " to the Req shop", LogTextColor.Cyan);
			}
		}
	}

	public void GenerateAssortItems(MongoId itemId, TemplateItem rootItemDb, TraderAssort baseTraderAssort, int minStockCount, int maxStockCount)
	{
		List<Item> list = new List<Item>
		{
			new Item
			{
				Id = new MongoId(),
				Template = itemId,
				ParentId = "hideout",
				SlotId = "hideout",
				Upd = new Upd
				{
					StackObjectsCount = helpers.GenRandomCount(minStockCount, maxStockCount)
				}
			}
		};
		if (itemHelper.IsOfBaseclass(itemId, BaseClasses.AMMO_BOX) && list.Count == 1)
		{
			itemHelper.AddCartridgesToAmmoBox(list, rootItemDb);
		}
		ItemExtensions.RemapRootItemId(list, (MongoId?)null);
		if (list.Count > 1)
		{
			itemHelper.ReparentItemAndChildren(list[0], list);
			list[0].ParentId = "hideout";
		}
		double cost = Math.Round(helpers.GetStackedItemPrice(itemId, list)!.Value);
		double finalItemCost = GetFinalItemCost(cost);
		MongoId moneyType = GetMoneyType(cost);
		BarterScheme val = new BarterScheme
		{
			Count = finalItemCost,
			Template = moneyType
		};
		Dictionary<MongoId, List<List<BarterScheme>>> barterScheme = baseTraderAssort.BarterScheme;
		MongoId id = list[0].Id;
		List<List<BarterScheme>> list2 = new List<List<BarterScheme>>(1);
		CollectionsMarshal.SetCount(list2, 1);
		ref List<BarterScheme> reference = ref CollectionsMarshal.AsSpan(list2)[0];
		List<BarterScheme> list3 = new List<BarterScheme>(1);
		CollectionsMarshal.SetCount(list3, 1);
		CollectionsMarshal.AsSpan(list3)[0] = val;
		reference = list3;
		barterScheme[id] = list2;
		baseTraderAssort.Items.AddRange(list);
		baseTraderAssort.LoyalLevelItems[list[0].Id] = helpers.GenRandomCount(1, 4);
	}

	public double GetFinalItemCost(double cost)
	{
		double num = 53999.0;
		if (cost >= num)
		{
			return GetSlipCost(cost);
		}
		return GetCoinCost(cost);
	}

	public MongoId GetMoneyType(double cost)
	{
		double num = 53999.0;
		if (cost >= num)
		{
			return (MongoId)"668b3c71042c73c6f9b00704";
		}
		return (MongoId)"66292e79a4d9da25e683ab55";
	}

	public double GetCoinCost(double cost)
	{
		double num = Math.Round(cost / 175.0);
		if (num >= 1.0)
		{
			return num;
		}
		return 1.0;
	}

	public double GetSlipCost(double cost)
	{
		double num = Math.Round(cost / 53999.0);
		if (num >= 1.0)
		{
			return num;
		}
		return 1.0;
	}
}

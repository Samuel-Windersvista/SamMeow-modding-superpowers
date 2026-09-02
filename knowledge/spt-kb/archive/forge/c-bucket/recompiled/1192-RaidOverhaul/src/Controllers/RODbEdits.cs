// RODbEdits: SPT 4.0 -> 4.1.2 迁移
// DatabaseService -> GlobalTable/LocationTable/BotTable/TemplateTable 注入
// ConfigServer.GetConfig<T>() -> 直接注入各配置类型
// XYZ -> System.Numerics.Vector3
// BossLocationSpawn/LooseLoot 等模型命名空间同步 4.1.2
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Path = System.IO.Path;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;

namespace RaidOverhaulMain.Controllers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class RODbEdits(
	ISptLogger<RODbEdits> logger,
	GlobalTable globalTable,
	LocationTable locationTable,
	BotTable botTable,
	TemplateTable templateTable,
	TradersTable tradersTable,
	LostOnDeathConfig lostOnDeathConfig,
	LocationConfig locationConfig,
	WeatherConfig weatherConfig,
	RagfairConfig ragfairConfig,
	ROHelpers roHelpers,
	RandomUtil randomUtil)
{
	private readonly LostOnDeathConfig _lostOnDeathConfig = lostOnDeathConfig;

	private readonly LocationConfig _locationConfig = locationConfig;

	private readonly WeatherConfig _weatherConfig = weatherConfig;

	private readonly RagfairConfig _ragfairConfig = ragfairConfig;

	private static ConfigFile? _config;

	private static AmmoStackList? _ammoList;

	public void PassDbConfigs(ConfigFile config, AmmoStackList ammoList)
	{
		_config = config;
		_ammoList = ammoList;
	}

	public void BuildDbEdits()
	{
		RaidChanges();
		WeightChanges();
		ItemChanges(roHelpers);
		StackChanges(roHelpers);
		TraderTweaks(roHelpers);
		if (_config.LootChangesEnabled)
		{
			LootChanges();
		}
		if (_config.ModifyEnemyBotHealth)
		{
			ModifyEnemyHealth();
		}
		if (_config.WeatherChangesEnabled && _config.WinterWonderland)
		{
			WeatherChangesWinterWonderland();
		}
		ROLogger.Log<RODbEdits>(logger, "Database Edits finished loading", LogTextColor.Magenta);
	}

	private void RaidChanges()
	{
		GlobalConfig configuration = globalTable.Configuration;
		Dictionary<string, Location> dictionary = locationTable.GetDictionary();
		if (_config.EnableExtendedRaids)
		{
			foreach (Location value in dictionary.Values)
			{
				value.Base.ExitAccessTime = _config.TimeLimit;
				value.Base.EscapeTimeLimit = _config.TimeLimit;
				value.Base.EscapeTimeLimitCoop = _config.TimeLimit;
				value.Base.EscapeTimeLimitPVE = _config.TimeLimit;
			}
		}
		if (_config.ReduceFoodAndHydroDegradeEnabled)
		{
			configuration.Health.Effects.Existence.EnergyDamage = _config.EnergyDecay;
			configuration.Health.Effects.Existence.HydrationDamage = _config.HydroDecay;
		}
		if (_config.ChangeAirdropValuesEnabled)
		{
			foreach (Location value2 in dictionary.Values)
			{
				AirdropParameter? val = value2.Base.AirdropParameters?.FirstOrDefault();
				if (val == null)
				{
					continue;
				}
				if (value2.Base.Id == "bigmap")
				{
					val.PlaneAirdropChance = _config.Customs;
				}
				if (value2.Base.Id == "woods")
				{
					val.PlaneAirdropChance = _config.Woods;
				}
				if (value2.Base.Id == "lighthouse")
				{
					val.PlaneAirdropChance = _config.Lighthouse;
				}
				if (value2.Base.Id == "shoreline")
				{
					val.PlaneAirdropChance = _config.Shoreline;
				}
				if (value2.Base.Id == "interchange")
				{
					val.PlaneAirdropChance = _config.Interchange;
				}
				if (value2.Base.Id == "rezervbase")
				{
					val.PlaneAirdropChance = _config.Reserve;
				}
				if (value2.Base.Id == "tarkovstreets")
				{
					val.PlaneAirdropChance = _config.Streets;
				}
				if (value2.Base.Id == "sandbox")
				{
					val.PlaneAirdropChance = _config.GroundZero;
				}
				if (value2.Base.Id == "sandbox_high")
				{
					val.PlaneAirdropChance = _config.GroundZero;
				}
			}
		}
		if (_config.SaveQuestItems)
		{
			_lostOnDeathConfig.QuestItems = false;
		}
		if (_config.NoRunThrough)
		{
			configuration.Exp.MatchEnd.SurvivedExperienceRequirement = 0;
			configuration.Exp.MatchEnd.SurvivedSecondsRequirement = 0;
		}
	}

	private void WeightChanges()
	{
		GlobalConfig configuration = globalTable.Configuration;
		if (!_config.WeightChangesEnabled)
		{
			return;
		}
		float mult = (float)_config.WeightMultiplier;
		// 4.1.2 Vector3 为 readonly struct（init 属性），不可原地修改 X/Y，需重建
		configuration.Stamina.BaseOverweightLimits = new Vector3(configuration.Stamina.BaseOverweightLimits.X * mult, configuration.Stamina.BaseOverweightLimits.Y * mult, configuration.Stamina.BaseOverweightLimits.Z);
		configuration.Stamina.WalkOverweightLimits = new Vector3(configuration.Stamina.WalkOverweightLimits.X * mult, configuration.Stamina.WalkOverweightLimits.Y * mult, configuration.Stamina.WalkOverweightLimits.Z);
		configuration.Stamina.WalkSpeedOverweightLimits = new Vector3(configuration.Stamina.WalkSpeedOverweightLimits.X * mult, configuration.Stamina.WalkSpeedOverweightLimits.Y * mult, configuration.Stamina.WalkSpeedOverweightLimits.Z);
		configuration.Stamina.SprintOverweightLimits = new Vector3(configuration.Stamina.SprintOverweightLimits.X * mult, configuration.Stamina.SprintOverweightLimits.Y * mult, configuration.Stamina.SprintOverweightLimits.Z);
		configuration.Inertia.InertiaLimits = new Vector3(configuration.Inertia.InertiaLimits.X, configuration.Inertia.InertiaLimits.Y * mult, configuration.Inertia.InertiaLimits.Z);
	}

	private void LootChanges()
	{
		Dictionary<string, Location> dictionary = locationTable.GetDictionary();
		foreach (KeyValuePair<string, double> item in _locationConfig.LooseLootMultiplier)
		{
			_locationConfig.LooseLootMultiplier[item.Key] = _config.LooseLootMultiplier;
		}
		foreach (KeyValuePair<string, double> item2 in _locationConfig.StaticLootMultiplier)
		{
			_locationConfig.StaticLootMultiplier[item2.Key] = _config.StaticLootMultiplier;
		}
		foreach (KeyValuePair<string, Location> item3 in dictionary)
		{
			string id = item3.Key;
			item3.Value.LooseLoot?.AddTransformer(lazyLoadedLooseLootData =>
			{
				ModifyMarkedRoomLoot(id, lazyLoadedLooseLootData);
				return lazyLoadedLooseLootData;
			});
		}
	}

	private void TraderTweaks(ROHelpers helpers)
	{
		Dictionary<MongoId, Quest> quests = templateTable.Quests;
		Dictionary<MongoId, Trader> traders = tradersTable;
		if (_config.InsuranceChangesEnabled)
		{
			traders[(MongoId)"54cb50c76803fa8b248b4571"].Base.Insurance.MinReturnHour = _config.PraporMinReturn;
			traders[(MongoId)"54cb50c76803fa8b248b4571"].Base.Insurance.MaxReturnHour = _config.PraporMaxReturn;
			traders[(MongoId)"54cb57776803fa99248b456e"].Base.Insurance.MinReturnHour = _config.TherapistMinReturn;
			traders[(MongoId)"54cb57776803fa99248b456e"].Base.Insurance.MaxReturnHour = _config.TherapistMaxReturn;
		}
		if (_config.Ll1Items && _config.EnableRequisitionOffice)
		{
			string text = helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps);
			foreach (KeyValuePair<MongoId, int> loyalLevelItem in traders[(MongoId)text].Assort.LoyalLevelItems)
			{
				traders[(MongoId)text].Assort.LoyalLevelItems[loyalLevelItem.Key] = 1;
			}
		}
		if (_config.DisableFleaBlacklist)
		{
			_ragfairConfig.Dynamic.Blacklist.EnableBsgList = false;
		}
		if (!_config.RemoveFirRequirementsForQuests)
		{
			return;
		}
		foreach (KeyValuePair<MongoId, Quest> item in quests)
		{
			Quest val = quests[item.Key];
			if (val.Conditions.AvailableForFinish == null)
			{
				continue;
			}
			foreach (QuestCondition item2 in val.Conditions.AvailableForFinish)
			{
				if (item2.OnlyFoundInRaid == true)
				{
					item2.OnlyFoundInRaid = false;
				}
			}
		}
	}

	private void ModifyEnemyHealth()
	{
		foreach (var (_, val2) in botTable.Types)
		{
			if (val2 == null)
			{
				continue;
			}
			foreach (BodyPart bodyPart in val2.BotHealth.BodyParts)
			{
				SetHealth(bodyPart.Chest, 85.0);
				SetHealth(bodyPart.Head, 35.0);
				SetHealth(bodyPart.Stomach, 70.0);
				SetHealth(bodyPart.LeftLeg, 65.0);
				SetHealth(bodyPart.RightLeg, 65.0);
				SetHealth(bodyPart.LeftArm, 60.0);
				SetHealth(bodyPart.RightArm, 60.0);
			}
		}
	}

	private static void SetHealth(MinMax<double> botHealth, double setValue)
	{
		botHealth.Min = Math.Min(botHealth.Min, setValue);
		botHealth.Max = botHealth.Min;
	}

	private void ItemChanges(ROHelpers helpers)
	{
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		TemplateItem val = items[(MongoId)helpers.FetchIdFromMap("POCKETS_SPECIAL", ClassMaps.AllItemList)];
		TemplateItem val2 = items[(MongoId)helpers.FetchIdFromMap("POCKETS_UNHEARD", ClassMaps.AllItemList)];
		foreach (KeyValuePair<MongoId, TemplateItem> item in items)
		{
			TemplateItem val3 = items[item.Key];
			if (_config.LootableMelee && val3.Parent == BaseClasses.KNIFE)
			{
				val3.Properties.Unlootable = false;
				val3.Properties.UnlootableFromSide = Array.Empty<PlayerSideMask>();
			}
			if (_config.LootableArmbands && val3.Parent == BaseClasses.ARM_BAND)
			{
				val3.Properties.Unlootable = false;
				val3.Properties.UnlootableFromSide = Array.Empty<PlayerSideMask>();
			}
			TemplateItemProperties? properties = val3.Properties;
			if (properties != null && properties.BlocksEarpiece == true)
			{
				val3.Properties.BlocksEarpiece = false;
			}
			TemplateItemProperties? properties2 = val3.Properties;
			if (properties2 != null && properties2.BlocksFaceCover == true)
			{
				val3.Properties.BlocksFaceCover = false;
			}
			if (val3.Id == (MongoId)helpers.FetchIdFromMap("ARMOREDEQUIPMENT_TK_HEAVY_TROOPER", ClassMaps.AllItemList))
			{
				val3.Properties.ArmorClass = 4;
			}
		}
		if (_config.PocketChangesEnabled)
		{
			TemplateItemProperties? properties3 = val.Properties;
			List<Grid>? list = properties3?.Grids?.ToList();
			TemplateItemProperties? properties4 = val2.Properties;
			List<Grid>? list2 = properties4?.Grids?.ToList();
			if (list != null)
			{
				list[0].Properties.CellsH = _config.Pocket1Horizontal;
				list[0].Properties.CellsV = _config.Pocket1Vertical;
				list[1].Properties.CellsH = _config.Pocket2Horizontal;
				list[1].Properties.CellsV = _config.Pocket2Vertical;
				list[2].Properties.CellsH = _config.Pocket3Horizontal;
				list[2].Properties.CellsV = _config.Pocket3Vertical;
				list[3].Properties.CellsH = _config.Pocket4Horizontal;
				list[3].Properties.CellsV = _config.Pocket4Vertical;
				val.Properties.Grids = list;
			}
			if (list2 != null)
			{
				list2[0].Properties.CellsH = _config.Pocket1Horizontal;
				list2[0].Properties.CellsV = _config.Pocket1Vertical;
				list2[1].Properties.CellsH = _config.Pocket2Horizontal;
				list2[1].Properties.CellsV = _config.Pocket2Vertical;
				list2[2].Properties.CellsH = _config.Pocket3Horizontal;
				list2[2].Properties.CellsV = _config.Pocket3Vertical;
				list2[3].Properties.CellsH = _config.Pocket4Horizontal;
				list2[3].Properties.CellsV = _config.Pocket4Vertical;
				val2.Properties.Grids = list2;
			}
		}
		if (_config.SpecialSlotChanges)
		{
			foreach (Slot slot in val.Properties.Slots)
			{
				slot.Properties.Filters.FirstOrDefault().Filter = new HashSet<MongoId> { (MongoId)"54009119af1c881c07000029" };
			}
			foreach (Slot slot2 in val2.Properties.Slots)
			{
				slot2.Properties.Filters.FirstOrDefault().Filter = new HashSet<MongoId> { (MongoId)"54009119af1c881c07000029" };
			}
		}
		if (_config.HolsterAnything)
		{
			TemplateItemProperties? properties5 = items[(MongoId)"55d7217a4bdc2d86028b456d"].Properties;
			IEnumerable<Slot>? enumerable = properties5?.Slots;
			string text = helpers.FetchIdFromMap("Holster", ClassMaps.SlotIds);
			if (enumerable == null)
			{
				return;
			}
			foreach (Slot item2 in enumerable)
			{
				SlotProperties? properties6 = item2.Properties;
				List<SlotFilter>? list3 = properties6?.Filters?.ToList();
				if (list3 == null)
				{
					continue;
				}
				string? id = item2.Id;
				if (id != null && id == text)
				{
					SlotFilter? obj = list3.FirstOrDefault();
					obj?.Filter?.Add((MongoId)"5422acb9af1c889c16000029");
					item2.Properties.Filters = list3;
				}
			}
		}
		if (_config.LowerExamineTime)
		{
			foreach (KeyValuePair<MongoId, TemplateItem> item3 in items)
			{
				items[item3.Key].Properties.ExamineTime = 0.1;
			}
		}
		foreach (KeyValuePair<string, BotType> type in botTable.Types)
		{
			BotType? obj2 = type.Value;
			if (obj2 != null)
			{
				obj2.BotInventory.Items.Pockets.TryAdd((MongoId)"668b3c71042c73c6f9b00704", 1.0);
			}
		}
		foreach (KeyValuePair<string, BotType> type2 in botTable.Types)
		{
			BotType? obj3 = type2.Value;
			if (obj3 != null)
			{
				obj3.BotInventory.Items.Pockets.TryAdd((MongoId)"66292e79a4d9da25e683ab55", 1.0);
			}
		}
		helpers.AddToCases(new string[14]
		{
			"5732ee6a24597719ae0c0281", "544a11ac4bdc2d470e8b456a", "5857a8b324597729ab0a0e7d", "5857a8bc2459772bad15db29", "59db794186f77448bc595262", "5c093ca986f7740a1867ab12", "6621b12c9f46c3eb4a0c8f40", "6621b143edb81061ceb5d7cc", "6621b177ce1b117550362db5", "6621b1895c9cd0794d536d14",
			"6621b1986f4ebd47e39eacb5", "6621b1b3166c301c04facfc8", "666361eff60f4ea5a464eb70", "666362befb4578a9f2450bd8"
		}, (MongoId)"64d4b23dc1b37504b41ac2b6");
		helpers.AddToCases(new string[4] { "5783c43d2459774bbe137486", "60b0f6c058e0b0481a09ad11", "590c60fc86f77412b13fddcf", "5d235bb686f77443f4331278" }, (MongoId)"59f32c3b86f77472a31742f0");
		helpers.AddToCases(new string[4] { "5783c43d2459774bbe137486", "60b0f6c058e0b0481a09ad11", "590c60fc86f77412b13fddcf", "5d235bb686f77443f4331278" }, (MongoId)"59f32bb586f774757e1e8442");
		if (_config.ChangeBackpackSizes)
		{
			helpers.ModifyContainerSize((MongoId)"5df8a4d786f77412672a1e3b", 6, 12);
			helpers.ModifyContainerSize((MongoId)"628bc7fb408e2b2e9c0801b1", 6, 11);
			helpers.ModifyContainerSize((MongoId)"5c0e774286f77468413cc5b2", 6, 10);
			helpers.ModifyContainerSize((MongoId)"5e4abc6786f77406812bd572", 6, 9);
			helpers.ModifyContainerSize((MongoId)"5e997f0b86f7741ac73993e2", 6, 6);
			helpers.ModifyContainerSize((MongoId)"5ab8ebf186f7742d8b372e80", 6, 9);
			helpers.ModifyContainerSize((MongoId)"61b9e1aaef9a1b5d6a79899a", 6, 9);
			helpers.ModifyContainerSize((MongoId)"59e763f286f7742ee57895da", 6, 9);
			helpers.ModifyContainerSize((MongoId)"639346cc1c8f182ad90c8972", 6, 8);
			helpers.ModifyContainerSize((MongoId)"628e1ffc83ec92260c0f437f", 6, 6);
			helpers.ModifyContainerSize((MongoId)"62a1b7fbc30cfa1d366af586", 6, 6);
			helpers.ModifyContainerSize((MongoId)"5b44c6ae86f7742d1627baea", 6, 6);
			helpers.ModifyContainerSize((MongoId)"545cdae64bdc2d39198b4568", 6, 6);
			helpers.ModifyContainerSize((MongoId)"5f5e467b0bc58666c37e7821", 6, 6);
			helpers.ModifyContainerSize((MongoId)"618bb76513f5097c8d5aa2d5", 6, 5);
			helpers.ModifyContainerSize((MongoId)"619cf0335771dd3c390269ae", 6, 5);
			helpers.ModifyContainerSize((MongoId)"60a272cc93ef783291411d8e", 6, 5);
			helpers.ModifyContainerSize((MongoId)"618cfae774bb2d036a049e7c", 6, 5);
			helpers.ModifyContainerSize((MongoId)"6034d103ca006d2dca39b3f0", 4, 8);
			helpers.ModifyContainerSize((MongoId)"6038d614d10cbf667352dd44", 4, 8);
			helpers.ModifyContainerSize((MongoId)"60a2828e8689911a226117f9", 6, 5);
			helpers.ModifyContainerSize((MongoId)"5e9dcf5986f7746c417435b3", 5, 5);
			helpers.ModifyContainerSize((MongoId)"56e335e4d2720b6c058b456d", 5, 5);
			helpers.ModifyContainerSize((MongoId)"5ca20d5986f774331e7c9602", 5, 5);
			helpers.ModifyContainerSize((MongoId)"544a5cde4bdc2d39388b456b", 4, 5);
			helpers.ModifyContainerSize((MongoId)"56e33634d2720bd8058b456b", 5, 3);
			helpers.ModifyContainerSize((MongoId)"5f5e45cc5021ce62144be7aa", 3, 5);
			helpers.ModifyContainerSize((MongoId)"56e33680d2720be2748b4576", 4, 3);
			helpers.ModifyContainerSize((MongoId)"5ab8ee7786f7742d8f33f0b9", 3, 4);
			helpers.ModifyContainerSize((MongoId)"5ab8f04f86f774585f4237d8", 3, 3);
			helpers.ModifyContainerSize((MongoId)"66a9f98f3bd5a41b162030f4", 6, 9);
			helpers.ModifyContainerSize((MongoId)"66b5f247af44ca0014063c02", 5, 5);
			helpers.ModifyContainerSize((MongoId)"66b5f22b78bbc0200425f904", 6, 6);
		}
	}

	private void StackChanges(ROHelpers helpers)
	{
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		if (_config.AdvancedStackTuningEnabled && !_config.BasicStackTuningEnabled)
		{
			string[] shotgunList = _ammoList.ShotgunList;
			foreach (string text in shotgunList)
			{
				items[(MongoId)text].Properties.StackMaxSize = _config.ShotgunStack;
			}
			shotgunList = _ammoList.UbglList;
			foreach (string text2 in shotgunList)
			{
				items[(MongoId)text2].Properties.StackMaxSize = _config.FlaresAndUbgl;
			}
			shotgunList = _ammoList.SniperList;
			foreach (string text3 in shotgunList)
			{
				items[(MongoId)text3].Properties.StackMaxSize = _config.SniperStack;
			}
			shotgunList = _ammoList.SmgList;
			foreach (string text4 in shotgunList)
			{
				items[(MongoId)text4].Properties.StackMaxSize = _config.SmgStack;
			}
			shotgunList = _ammoList.RifleList;
			foreach (string text5 in shotgunList)
			{
				items[(MongoId)text5].Properties.StackMaxSize = _config.RifleStack;
			}
		}
		if (_config.BasicStackTuningEnabled && !_config.AdvancedStackTuningEnabled)
		{
			foreach (KeyValuePair<MongoId, TemplateItem> item in items)
			{
				if (items[item.Key].Parent == (MongoId)helpers.FetchIdFromMap("AMMO", ClassMaps.ItemBaseClasses))
				{
					TemplateItemProperties? properties = items[item.Key].Properties;
					if (properties != null && properties.StackMaxSize.HasValue)
					{
						items[item.Key].Properties.StackMaxSize *= _config.StackMultiplier;
					}
				}
			}
		}
		if (_config.BasicStackTuningEnabled && _config.AdvancedStackTuningEnabled)
		{
			ROLogger.Log<RODbEdits>(logger, "Error multiplying your ammo stacks. Make sure you only have ONE of the Stack Tuning options enabled", LogTextColor.Red);
		}
		if (!_config.MoneyStackMultiplierEnabled)
		{
			return;
		}
		foreach (KeyValuePair<MongoId, TemplateItem> item2 in items)
		{
			if (items[item2.Key].Parent == (MongoId)helpers.FetchIdFromMap("MONEY", ClassMaps.ItemBaseClasses))
			{
				TemplateItemProperties? properties2 = items[item2.Key].Properties;
				if (properties2 != null && properties2.StackMaxSize.HasValue)
				{
					items[item2.Key].Properties.StackMaxSize *= _config.MoneyMultiplier;
				}
			}
		}
	}

	private static void ModifyMarkedRoomLoot(string location, LooseLoot looseLoot)
	{
		if (looseLoot.Spawnpoints == null)
		{
			return;
		}
		foreach (Spawnpoint spawnpoint in looseLoot.Spawnpoints)
		{
			SpawnpointTemplate? template = spawnpoint.Template;
			Vector3? val = template?.Position;
			if (string.Equals(location.ToLower(), "bigmap"))
			{
				if (val != null && val.Value.X > 180.0f && val.Value.X < 185.0f && val.Value.Y > 6.0f && val.Value.Y < 7.0f && val.Value.Z > 180.0f && val.Value.Z < 185.0f)
				{
					spawnpoint.Probability *= _config.MarkedRoomLootMultiplier;
					break;
				}
			}
			else if (string.Equals(location.ToLower(), "rezervbase"))
			{
				if (val != null && val.Value.X > -125.0f && val.Value.X < -120.0f && val.Value.Y > -15.0f && val.Value.Y < -14.0f && val.Value.Z > 25.0f && val.Value.Z < 30.0f)
				{
					spawnpoint.Probability *= _config.MarkedRoomLootMultiplier;
					break;
				}
				if (val != null && val.Value.X > -155.0f && val.Value.X < -150.0f && val.Value.Y > -9.0f && val.Value.Y < -8.0f && val.Value.Z > 70.0f && val.Value.Z < 75.0f)
				{
					spawnpoint.Probability *= _config.MarkedRoomLootMultiplier;
					break;
				}
				if (val != null && val.Value.X > 190.0f && val.Value.X < 195.0f && val.Value.Y > -6.0f && val.Value.Y < -5.0f && val.Value.Z > -230.0f && val.Value.Z < -225.0f)
				{
					spawnpoint.Probability *= _config.MarkedRoomLootMultiplier;
					break;
				}
			}
			else if (string.Equals(location.ToLower(), "tarkovstreets"))
			{
				if (val != null && val.Value.X > -133.0f && val.Value.X < -129.0f && val.Value.Y > 8.5f && val.Value.Y < 11.0f && val.Value.Z > 265.0f && val.Value.Z < 275.0f)
				{
					spawnpoint.Probability *= _config.MarkedRoomLootMultiplier;
					break;
				}
				if (val != null && val.Value.X > 186.0f && val.Value.X < 191.0f && val.Value.Y > -0.5f && val.Value.Y < 1.5f && val.Value.Z > 224.0f && val.Value.Z < 229.0f)
				{
					spawnpoint.Probability *= _config.MarkedRoomLootMultiplier;
					break;
				}
			}
			else if (string.Equals(location.ToLower(), "lighthouse"))
			{
				if (val != null && val.Value.X > 319.0f && val.Value.X < 330.0f && val.Value.Y > 5.0f && val.Value.Y < 6.5f && val.Value.Z > 482.0f && val.Value.Z < 489.0f)
				{
					spawnpoint.Probability *= _config.MarkedRoomLootMultiplier;
					break;
				}
			}
		}
	}

	public void WeatherChangesAllSeasons()
	{
		if (_config.AllSeasons && !_config.WinterWonderland && !_config.NoWinter && !_config.SeasonalProgression)
		{
			int num = randomUtil.GetInt(1, 100, false);
			if (num >= 1 && num <= 20)
			{
				_weatherConfig.OverrideSeason = (Season)0;
				ROLogger.Log<RODbEdits>(logger, "Summer is active.", LogTextColor.Magenta);
			}
			else if (num >= 21 && num <= 40)
			{
				_weatherConfig.OverrideSeason = (Season)1;
				ROLogger.Log<RODbEdits>(logger, "Autumn is active.", LogTextColor.Magenta);
			}
			else if (num >= 41 && num <= 60)
			{
				_weatherConfig.OverrideSeason = (Season)2;
				ROLogger.Log<RODbEdits>(logger, "Winter is coming.", LogTextColor.Magenta);
			}
			else if (num >= 61 && num <= 80)
			{
				_weatherConfig.OverrideSeason = (Season)3;
				ROLogger.Log<RODbEdits>(logger, "Spring is active.", LogTextColor.Magenta);
			}
			else if (num >= 81 && num <= 100)
			{
				_weatherConfig.OverrideSeason = (Season)6;
				ROLogger.Log<RODbEdits>(logger, "Storm is active.", LogTextColor.Magenta);
			}
		}
		else if ((_config.AllSeasons && _config.WinterWonderland) || (_config.NoWinter && _config.WinterWonderland) || (_config.SeasonalProgression && _config.WinterWonderland) || (_config.NoWinter && _config.SeasonalProgression) || (_config.NoWinter && _config.AllSeasons) || (_config.SeasonalProgression && _config.AllSeasons))
		{
			ROLogger.Log<RODbEdits>(logger, "Error modifying your weather. Make sure you only have ONE of the weather options enabled", LogTextColor.Red);
		}
	}

	public void WeatherChangesNoWinter()
	{
		if (_config.NoWinter && !_config.WinterWonderland && !_config.AllSeasons && !_config.SeasonalProgression)
		{
			int num = randomUtil.GetInt(1, 100, false);
			if (num >= 1 && num <= 25)
			{
				_weatherConfig.OverrideSeason = (Season)0;
				ROLogger.Log<RODbEdits>(logger, "Summer is active.", LogTextColor.Magenta);
				return;
			}
			if (num >= 26 && num <= 50)
			{
				_weatherConfig.OverrideSeason = (Season)1;
				ROLogger.Log<RODbEdits>(logger, "Autumn is active.", LogTextColor.Magenta);
				return;
			}
			if (num >= 51 && num <= 75)
			{
				_weatherConfig.OverrideSeason = (Season)3;
				ROLogger.Log<RODbEdits>(logger, "Spring is active.", LogTextColor.Magenta);
				return;
			}
			if (num >= 76 && num <= 100)
			{
				_weatherConfig.OverrideSeason = (Season)6;
				ROLogger.Log<RODbEdits>(logger, "Storm is active.", LogTextColor.Magenta);
			}
		}
		else if ((_config.AllSeasons && _config.WinterWonderland) || (_config.NoWinter && _config.WinterWonderland) || (_config.SeasonalProgression && _config.WinterWonderland) || (_config.NoWinter && _config.SeasonalProgression) || (_config.NoWinter && _config.AllSeasons) || (_config.SeasonalProgression && _config.AllSeasons))
		{
			ROLogger.Log<RODbEdits>(logger, "Error modifying your weather. Make sure you only have ONE of the weather options enabled", LogTextColor.Red);
		}
	}

	private void WeatherChangesWinterWonderland()
	{
		if (_config.WinterWonderland && !_config.AllSeasons && !_config.NoWinter && !_config.SeasonalProgression)
		{
			_weatherConfig.OverrideSeason = (Season)2;
			ROLogger.Log<RODbEdits>(logger, "Snow is active. It's a whole fuckin' winter wonderland out there.", LogTextColor.Magenta);
		}
		else if ((_config.AllSeasons && _config.WinterWonderland) || (_config.NoWinter && _config.WinterWonderland) || (_config.SeasonalProgression && _config.WinterWonderland) || (_config.NoWinter && _config.SeasonalProgression) || (_config.NoWinter && _config.AllSeasons) || (_config.SeasonalProgression && _config.AllSeasons))
		{
			ROLogger.Log<RODbEdits>(logger, "Error modifying your weather. Make sure you only have ONE of the weather options enabled", LogTextColor.Red);
		}
	}

	public void SeasonProgression(SeasonalProgression seasonProgressionFile, DebugFile debugConfig, Assembly assembly, ROHelpers helpers)
	{
		int seasonsProgression = seasonProgressionFile.SeasonsProgression;
		switch (seasonsProgression)
		{
		case 1:
		case 2:
		case 3:
			seasonsProgression++;
			_weatherConfig.OverrideSeason = (Season)3;
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, "Spring is active.", LogTextColor.Magenta);
			}
			break;
		case 4:
		case 5:
		case 6:
			seasonsProgression++;
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, "Storm is active.", LogTextColor.Magenta);
			}
			break;
		case 7:
		case 8:
		case 9:
			seasonsProgression++;
			_weatherConfig.OverrideSeason = (Season)0;
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, "Summer is active.", LogTextColor.Magenta);
			}
			break;
		case 10:
		case 11:
		case 12:
			seasonsProgression++;
			_weatherConfig.OverrideSeason = (Season)1;
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, "Autumn is active.", LogTextColor.Magenta);
			}
			break;
		case 13:
		case 14:
			seasonsProgression++;
			_weatherConfig.OverrideSeason = (Season)4;
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, "Autumn is active.", LogTextColor.Magenta);
			}
			break;
		case 15:
		case 16:
		case 17:
		case 18:
			seasonsProgression++;
			_weatherConfig.OverrideSeason = (Season)2;
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, "Winter is coming.", LogTextColor.Magenta);
			}
			break;
		default:
			_weatherConfig.OverrideSeason = (Season)5;
			seasonsProgression = 1;
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, "Defaulting to spring.", LogTextColor.Magenta);
			}
			break;
		}
		try
		{
			seasonProgressionFile.SeasonsProgression = seasonsProgression;
			helpers.WriteConfigFile(seasonProgressionFile, assembly, Path.Combine("db", "devFiles"), "seasonsProgressionFile.json");
			if (debugConfig.DebugMode)
			{
				ROLogger.Log<RODbEdits>(logger, $"Seasonal progress updated to {seasonsProgression}", LogTextColor.Cyan);
			}
		}
		catch (Exception ex)
		{
			ROLogger.LogError<RODbEdits>(logger, "Error writing season progression file: " + ex);
		}
	}

	// TradersTable 通过构造注入使用（Dictionary<MongoId, Trader>）
}

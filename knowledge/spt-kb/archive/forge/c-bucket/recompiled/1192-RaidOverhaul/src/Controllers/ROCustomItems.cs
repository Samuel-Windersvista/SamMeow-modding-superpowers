// ROCustomItems: SPT 4.0 -> 4.1.2 迁移
// DatabaseService -> TemplateTable 注入；ConfigServer.GetConfig<RagfairConfig>() -> 直接注入 RagfairConfig
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using WTTServerCommonLib.Services;

namespace RaidOverhaulMain.Controllers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROCustomItems(
	ISptLogger<ROCustomItems> logger,
	TemplateTable templateTable,
	RagfairConfig ragfairConfig,
	WTTCustomItemServiceExtended wttItemService,
	WTTCustomRigLayoutService wttRigLayoutService,
	ROHelpers roHelpers)
{
	private readonly RagfairConfig _ragfairConfig = ragfairConfig;

	private static ConfigFile? _config;

	public void PassCustomItemConfigs(ConfigFile config)
	{
		_config = config;
	}

	public async Task BuildCustomItems()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		await LoadCustomItems(assembly, roHelpers);
		wttRigLayoutService.CreateRigLayouts(assembly, "db/itemGen/customLayouts");
		ApplyFleaBlacklist();
		ApplyCasePushes();
		ROLogger.Log<ROCustomItems>(logger, "Custom Items finished loading", LogTextColor.Magenta);
	}

	private async Task LoadCustomItems(Assembly assembly, ROHelpers helpers)
	{
		await wttItemService.CreateCustomItems(assembly, "db/itemGen/currency");
		await wttItemService.CreateCustomItems(assembly, "db/itemGen/constItems");
		await wttItemService.CreateCustomItems(assembly, "db/itemGen/customKeys");
		await wttItemService.CreateCustomItems(assembly, "db/itemGen/cases");
		if (!_config.EnableCustomItems)
		{
			return;
		}
		if (helpers.CheckForMod("SPT-Realism"))
		{
			await wttItemService.CreateCustomItems(assembly, "db/itemGen/ammoRealism");
			ROLogger.Log<ROCustomItems>(logger, "Realism detected, modifying custom ammunition.", LogTextColor.Magenta);
		}
		else
		{
			await wttItemService.CreateCustomItems(assembly, "db/itemGen/ammo");
		}
		await wttItemService.CreateCustomItems(assembly, "db/itemGen/weapons");
		await wttItemService.CreateCustomItems(assembly, "db/itemGen/gear");
		ApplyFleaBlacklistCustomWeapons();
		BuildSlots(helpers);
	}

	private void ApplyFleaBlacklist()
	{
		string[] array = new string[12]
		{
			"67c957ce411e6263333a1c38", "6621b0dcbcfe66cdbbab48c7", "666361eff60f4ea5a464eb70", "6621b12c9f46c3eb4a0c8f40", "6621b143edb81061ceb5d7cc", "6621b177ce1b117550362db5", "6621b1895c9cd0794d536d14", "6621b1986f4ebd47e39eacb5", "6621b1b3166c301c04facfc8", "67c95a09708ee99e7a575da5",
			"66a2fc926af26cc365283f23", "66a2fc9886fbd5d38c5ca2a6"
		};
		foreach (string text in array)
		{
			_ragfairConfig.Dynamic.Blacklist.Custom.Add((MongoId)text);
		}
	}

	private void ApplyFleaBlacklistCustomWeapons()
	{
		string[] array = new string[3] { "6628f96fd59ab54dedb55801", "6628f76df1a913e3afc16360", "6628f8813e3fe94f5f035010" };
		foreach (string text in array)
		{
			_ragfairConfig.Dynamic.Blacklist.Custom.Add((MongoId)text);
		}
	}

	private void ApplyCasePushes()
	{
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>
		{
			["66292e79a4d9da25e683ab55"] = new string[5] { "5c093db286f7740a1b2617e3", "5d235bb686f77443f4331278", "5783c43d2459774bbe137486", "60b0f6c058e0b0481a09ad11", "59fb016586f7746d0d4b423a" },
			["668b3c71042c73c6f9b00704"] = new string[5] { "5c093db286f7740a1b2617e3", "5d235bb686f77443f4331278", "5783c43d2459774bbe137486", "60b0f6c058e0b0481a09ad11", "59fb016586f7746d0d4b423a" },
			["67c95a09708ee99e7a575da5"] = new string[5] { "5c093db286f7740a1b2617e3", "5d235bb686f77443f4331278", "5783c43d2459774bbe137486", "60b0f6c058e0b0481a09ad11", "59fb016586f7746d0d4b423a" },
			["66a2fc926af26cc365283f23"] = new string[5] { "5c093db286f7740a1b2617e3", "5d235bb686f77443f4331278", "5783c43d2459774bbe137486", "60b0f6c058e0b0481a09ad11", "59fafd4b86f7745ca07e1232" },
			["66a2fc9886fbd5d38c5ca2a6"] = new string[5] { "5c093db286f7740a1b2617e3", "5d235bb686f77443f4331278", "5783c43d2459774bbe137486", "60b0f6c058e0b0481a09ad11", "619cbf9e0a7c3a1a2731940a" }
		};
		foreach (KeyValuePair<string, string[]> item in dictionary)
		{
			string text = item.Key;
			foreach (string text2 in item.Value)
			{
				TemplateItemProperties? properties = items[(MongoId)text2].Properties;
				if (properties == null)
				{
					continue;
				}
				IEnumerable<Grid>? grids = properties.Grids;
				if (grids == null)
				{
					continue;
				}
				Grid? obj = grids.FirstOrDefault();
				if (obj == null)
				{
					continue;
				}
				GridProperties? properties2 = obj.Properties;
				if (properties2 == null)
				{
					continue;
				}
				IEnumerable<GridFilter>? filters = properties2.Filters;
				if (filters != null)
				{
					GridFilter? obj2 = filters.FirstOrDefault();
					obj2?.Filter?.Add((MongoId)text);
				}
			}
		}
	}

	private void BuildSlots(ROHelpers helpers)
	{
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		string text = helpers.FetchIdFromMap("Aug762", ClassMaps.CustomItemMap);
		string text2 = helpers.FetchIdFromMap("Stm46", ClassMaps.CustomItemMap);
		string text3 = helpers.FetchIdFromMap("Mcm4", ClassMaps.CustomItemMap);
		string text4 = helpers.FetchIdFromMap("Judge", ClassMaps.CustomItemMap);
		string text5 = helpers.FetchIdFromMap("Jury", ClassMaps.CustomItemMap);
		string text6 = helpers.FetchIdFromMap("Exec", ClassMaps.CustomItemMap);
		string text7 = helpers.FetchIdFromMap("Aug30Rd", ClassMaps.CustomItemMap);
		string text8 = helpers.FetchIdFromMap("Aug42Rd", ClassMaps.CustomItemMap);
		string text9 = helpers.FetchIdFromMap("Stm33Rd", ClassMaps.CustomItemMap);
		string text10 = helpers.FetchIdFromMap("Stm50Rd", ClassMaps.CustomItemMap);
		string text11 = helpers.FetchIdFromMap("StmRec", ClassMaps.CustomItemMap);
		string text12 = helpers.FetchIdFromMap("Mag300", ClassMaps.CustomItemMap);
		string text13 = helpers.FetchIdFromMap("Mag545", ClassMaps.CustomItemMap);
		string text14 = helpers.FetchIdFromMap("Mag57", ClassMaps.CustomItemMap);
		string text15 = helpers.FetchIdFromMap("Mag762", ClassMaps.CustomItemMap);
		string text16 = helpers.FetchIdFromMap("Mag939", ClassMaps.CustomItemMap);
		string text17 = helpers.FetchIdFromMap("Rec300", ClassMaps.CustomItemMap);
		string text18 = helpers.FetchIdFromMap("Rec545", ClassMaps.CustomItemMap);
		string text19 = helpers.FetchIdFromMap("Rec57", ClassMaps.CustomItemMap);
		string text20 = helpers.FetchIdFromMap("Rec762", ClassMaps.CustomItemMap);
		string text21 = helpers.FetchIdFromMap("Rec939", ClassMaps.CustomItemMap);
		string text22 = helpers.FetchIdFromMap("Judge17Rd", ClassMaps.CustomItemMap);
		string text23 = helpers.FetchIdFromMap("Judge33Rd", ClassMaps.CustomItemMap);
		string text24 = helpers.FetchIdFromMap("Judge50Rd", ClassMaps.CustomItemMap);
		string text25 = helpers.FetchIdFromMap("JudgeSlide", ClassMaps.CustomItemMap);
		string text26 = helpers.FetchIdFromMap("Jury20Rd", ClassMaps.CustomItemMap);
		string text27 = helpers.FetchIdFromMap("Jury25Rd", ClassMaps.CustomItemMap);
		string text28 = helpers.FetchIdFromMap("Jury50Rd", ClassMaps.CustomItemMap);
		string text29 = helpers.FetchIdFromMap("JuryRec", ClassMaps.CustomItemMap);
		string text30 = helpers.FetchIdFromMap("ExecAics", ClassMaps.CustomItemMap);
		string text31 = helpers.FetchIdFromMap("ExecPmag", ClassMaps.CustomItemMap);
		string text32 = helpers.FetchIdFromMap("ExecWyatt", ClassMaps.CustomItemMap);
		items[(MongoId)text].Properties.Slots.ElementAt(0).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId>
		{
			(MongoId)text7,
			(MongoId)text8
		};
		items[(MongoId)text2].Properties.Slots.ElementAt(1).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId>
		{
			(MongoId)text9,
			(MongoId)text10
		};
		items[(MongoId)text2].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId> { (MongoId)text11 };
		items[(MongoId)text3].Properties.Slots.ElementAt(1).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text12);
		items[(MongoId)text3].Properties.Slots.ElementAt(1).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text13);
		items[(MongoId)text3].Properties.Slots.ElementAt(1).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text14);
		items[(MongoId)text3].Properties.Slots.ElementAt(1).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text15);
		items[(MongoId)text3].Properties.Slots.ElementAt(1).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text16);
		items[(MongoId)text3].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text17);
		items[(MongoId)text3].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text18);
		items[(MongoId)text3].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text19);
		items[(MongoId)text3].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text20);
		items[(MongoId)text3].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter.Add((MongoId)text21);
		items[(MongoId)text4].Properties.Slots.ElementAt(3).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId>
		{
			(MongoId)text22,
			(MongoId)text23,
			(MongoId)text24
		};
		items[(MongoId)text4].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId> { (MongoId)text25 };
		items[(MongoId)text5].Properties.Slots.ElementAt(1).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId>
		{
			(MongoId)text26,
			(MongoId)text27,
			(MongoId)text28
		};
		items[(MongoId)text5].Properties.Slots.ElementAt(2).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId> { (MongoId)text29 };
		items[(MongoId)text6].Properties.Slots.ElementAt(0).Properties.Filters.ElementAt(0).Filter = new HashSet<MongoId>
		{
			(MongoId)text30,
			(MongoId)text31,
			(MongoId)text32
		};
	}
}

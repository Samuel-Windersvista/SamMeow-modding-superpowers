// ROTrader: SPT 4.0 -> 4.1.2 迁移
// ConfigServer.GetConfig<TraderConfig>() -> 直接注入 TraderConfig
using System.Reflection;
using Path = System.IO.Path;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;

namespace RaidOverhaulMain.Controllers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROTrader(
	ISptLogger<ROTrader> logger,
	TraderConfig traderConfig,
	ModHelper helper,
	ImageRouter imageRouter,
	ROTraderHelper traderHelper,
	ROAssortHelper assortHelper,
	ROQuestHelper questHelper,
	ROHelpers helpers)
{
	private readonly TraderConfig _traderConfig = traderConfig;

	private static ConfigFile? _config;

	private static DebugFile? _debugConfig;

	public void PassTraderConfigs(ConfigFile config, DebugFile debugConfig)
	{
		_config = config;
		_debugConfig = debugConfig;
	}

	public void BuildTrader()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string dbPath = Path.Combine(helper.GetAbsolutePathToModFolder(assembly), "db");
		string questPathEnabled = Path.Combine(dbPath, "questFiles", "bossEnabled");
		string questPathDisabled = Path.Combine(dbPath, "questFiles", "bossDisabled");
		string imagePath = Path.Combine(dbPath, "res", "Reqs.jpg");
		TraderBase? baseEnabled = helper.GetJsonDataFromFile<TraderBase>(dbPath, "baseBossEnabled.json");
		TraderBase? baseDisabled = helper.GetJsonDataFromFile<TraderBase>(dbPath, "baseBossDisabled.json");
		if (_config.EnableRequisitionOffice)
		{
			if (_config.EnableCustomBoss)
			{
				imageRouter.AddRoute(baseEnabled?.Avatar?.Replace(".jpg", "") ?? string.Empty, imagePath);
				traderHelper.SetTraderUpdateTime(_traderConfig, baseEnabled!, 3600, 7200);
				traderHelper.AddTraderWithEmptyAssortToDb(baseEnabled!);
				traderHelper.AddTraderToLocales(baseEnabled!, "Requisitions Office", "A collection of Ex-PMC's and rogue Scavs who formed a group to aid others in Tarkov. They routinely scour the battlefield for any leftover supplies and aren't afraid to fight their old comrades for it. They may not be the most trustworthy but they do have some much needed provisions in stock.");
				if (_config.EnableCustomItems)
				{
					assortHelper.AddCustomItemsToTraderShop(helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps), _debugConfig!);
				}
				assortHelper.GenerateTraderAssorts(helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps), _debugConfig!);
				questHelper.CreateCustomQuests(assembly, questPathEnabled);
			}
			else
			{
				imageRouter.AddRoute(baseDisabled?.Avatar?.Replace(".jpg", "") ?? string.Empty, imagePath);
				traderHelper.SetTraderUpdateTime(_traderConfig, baseDisabled!, 3600, 7200);
				traderHelper.AddTraderWithEmptyAssortToDb(baseDisabled!);
				traderHelper.AddTraderToLocales(baseDisabled!, "Requisitions Office", "A collection of Ex-PMC's and rogue Scavs who formed a group to aid others in Tarkov. They routinely scour the battlefield for any leftover supplies and aren't afraid to fight their old comrades for it. They may not be the most trustworthy but they do have some much needed provisions in stock.");
				if (_config.EnableCustomItems)
				{
					assortHelper.AddCustomItemsToTraderShop(helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps), _debugConfig!);
				}
				assortHelper.GenerateTraderAssorts(helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps), _debugConfig!);
				questHelper.CreateCustomQuests(assembly, questPathDisabled);
			}
			ROLogger.Log<ROTrader>(logger, "Requisition Shop finished loading", LogTextColor.Magenta);
		}
		if (_config.EnableCustomItems && !_config.EnableRequisitionOffice)
		{
			assortHelper.AddCustomItemsToTraderShop(helpers.FetchIdFromMap("Peacekeeper", ClassMaps.TraderMaps), _debugConfig!);
			ROLogger.Log<ROTrader>(logger, "Added custom items to Peacekeeper", LogTextColor.Magenta);
		}
	}
}

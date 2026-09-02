// ROStaticRouter: SPT 4.0 -> 4.1.2 迁移
// RouteAction 动作签名增加 CancellationToken 参数（4.1.2 变更）
// DatabaseService.GetTrader -> TradersTable.GetTrader
// TraderHelper 命名空间: Services -> Helpers.Traders
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;
using RaidOverhaulMain.Callbacks;
using RaidOverhaulMain.Controllers;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Helpers.Traders;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace RaidOverhaulMain.Routers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROStaticRouter : StaticRouter
{
	private static ConfigFile? _config;

	private static DebugFile? _debugConfig;

	private static EventsConfigFile? _eventsConfig;

	private static SeasonalProgression? _seasonsConfig;

	private static LegionProgression? _legionConfig;

	private static RODbEdits? _dbController;

	private static TradersTable? _tradersTable;

	private static ROHelpers? _helpers;

	private static ROBossHelper? _bossHelper;

	private static ModHelper? _modHelper;

	private static TraderHelper? _traderHelper;

	private static TransferRequestCallbacks? _transferRequestCallbacks;

	private static LogToServerRequestCallbacks? _serverLogCallbacks;

	private static ISptLogger<ROStaticRouter>? _logger;

	public ROStaticRouter(ISptLogger<ROStaticRouter> logger, JsonUtil jsonUtil, TraderHelper traderHelper, TradersTable tradersTable, ModHelper modHelper, ROHelpers helper, ROBossHelper bossHelper, RODbEdits dbController, TransferRequestCallbacks transferRequestCallbacks, LogToServerRequestCallbacks serverLogCallbacks)
		: base(jsonUtil, GetCustomRoutes())
	{
		_helpers = helper;
		_bossHelper = bossHelper;
		_dbController = dbController;
		_tradersTable = tradersTable;
		_modHelper = modHelper;
		_traderHelper = traderHelper;
		_transferRequestCallbacks = transferRequestCallbacks;
		_serverLogCallbacks = serverLogCallbacks;
		_logger = logger;
	}

	public void PassRouterConfigs(ConfigFile config, SeasonalProgression seasonsConfig, DebugFile debugConfig, EventsConfigFile eventsConfig, LegionProgression legionConfig)
	{
		_config = config;
		_seasonsConfig = seasonsConfig;
		_debugConfig = debugConfig;
		_eventsConfig = eventsConfig;
		_legionConfig = legionConfig;
	}

	private static List<RouteAction> GetCustomRoutes()
	{
		int num = 10;
		List<RouteAction> list = new List<RouteAction>(num);
		list.Add(new RouteAction<EmptyRequestData>("/client/game/start", (string _, EmptyRequestData _, MongoId sessionId, string? output, CancellationToken _) => HandleProfileRoute(sessionId, output)));
		list.Add(new RouteAction<EmptyRequestData>("/RaidOverhaul/GetEventConfig", (string _, EmptyRequestData _, MongoId _, string? _, CancellationToken _) => HandleRoute(_eventsConfig)));
		list.Add(new RouteAction<EmptyRequestData>("/RaidOverhaul/GetServerConfig", (string _, EmptyRequestData _, MongoId _, string? _, CancellationToken _) => HandleRoute(_config)));
		list.Add(new RouteAction<EmptyRequestData>("/RaidOverhaul/GetWeatherConfig", (string _, EmptyRequestData _, MongoId _, string? _, CancellationToken _) => HandleRoute(_seasonsConfig)));
		list.Add(new RouteAction<EmptyRequestData>("/RaidOverhaul/GetDebugConfig", (string _, EmptyRequestData _, MongoId _, string? _, CancellationToken _) => HandleRoute(_debugConfig)));
		list.Add(new RouteAction<EmptyRequestData>("/RaidOverhaul/GetLegionConfig", (string _, EmptyRequestData _, MongoId _, string? _, CancellationToken _) => HandleRoute(_legionConfig)));
		list.Add(new RouteAction<LogToServerRequestData>("/RaidOverhaul/LogToServer", (string _, LogToServerRequestData info, MongoId _, string? _, CancellationToken _) => _serverLogCallbacks.LogToServer<ROStaticRouter>(info, _logger)));
		list.Add(new RouteAction<TransferRequestData>("/RaidOverhaul/TransferItemRequests", (string _, TransferRequestData info, MongoId sessionId, string? _, CancellationToken _) => _transferRequestCallbacks.ReceiveAndSendItems(info, sessionId)));
		list.Add(new RouteAction<StartLocalRaidRequestData>("/client/match/local/start", (string _, StartLocalRaidRequestData _, MongoId _, string? output, CancellationToken _) => HandleStandardWeatherRoute(output)));
		list.Add(new RouteAction<EndLocalRaidRequestData>("/client/match/local/end", (string _, EndLocalRaidRequestData info, MongoId sessionId, string? output, CancellationToken _) => HandleROProgression(info, sessionId, output)));
		return list;
	}

	private static ValueTask<string> HandleRoute<T>(T? config)
	{
		return new ValueTask<string>(JsonSerializer.Serialize(config));
	}

	private static ValueTask<string> HandleProfileRoute(MongoId sessionId, string? output)
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string absolutePathToModFolder = _modHelper.GetAbsolutePathToModFolder(assembly);
		string[] buffer = new string[8];
		buffer[0] = absolutePathToModFolder;
		buffer[1] = "../";
		buffer[2] = "../";
		buffer[3] = "../";
		buffer[4] = "../";
		buffer[5] = "BepInEx";
		buffer[6] = "plugins";
		buffer[7] = "Fika";
		string path = Path.Combine(buffer);
		if (_config.BackupProfile && !Directory.Exists(path))
		{
			Task.Run(() => _helpers.ProfileBackup(sessionId, assembly));
		}
		return new ValueTask<string>(output ?? string.Empty);
	}

	private static ValueTask<string> HandleStandardWeatherRoute(string? output)
	{
		if (_config.WeatherChangesEnabled)
		{
			if (_config.NoWinter && !_config.AllSeasons && !_config.SeasonalProgression && !_config.WinterWonderland)
			{
				_dbController.WeatherChangesNoWinter();
			}
			if (_config.AllSeasons && _config.NoWinter && !_config.SeasonalProgression && !_config.WinterWonderland)
			{
				_dbController.WeatherChangesAllSeasons();
			}
		}
		return new ValueTask<string>(output ?? string.Empty);
	}

	private static ValueTask<string> HandleROProgression(EndLocalRaidRequestData info, MongoId sessionId, string? output)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		if (_config.WeatherChangesEnabled)
		{
			if (_config.SeasonalProgression && !_config.AllSeasons && !_config.NoWinter && !_config.WinterWonderland)
			{
				_dbController.SeasonProgression(_seasonsConfig, _debugConfig, executingAssembly, _helpers);
			}
			if ((_config.AllSeasons && _config.WinterWonderland) || (_config.NoWinter && _config.WinterWonderland) || (_config.SeasonalProgression && _config.WinterWonderland) || (_config.NoWinter && _config.SeasonalProgression) || (_config.NoWinter && _config.AllSeasons) || (_config.SeasonalProgression && _config.AllSeasons))
			{
				ROLogger.Log(_logger, "Error modifying your weather. Make sure you only have ONE of the weather options enabled", LogTextColor.Red);
			}
		}
		if (_config.EnableRequisitionOffice)
		{
			if (_config.Ll1Items)
			{
				HandleAssortLlItems(_tradersTable.GetTrader((MongoId)_helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps))!.Assort.LoyalLevelItems);
			}
			HandleREStatusRep(info, sessionId, (MongoId)_helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps));
			HandleBossRep(info, sessionId, (MongoId)_helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps));
		}
		if (!_config.EnableRequisitionOffice)
		{
			HandleREStatusRep(info, sessionId, (MongoId)_helpers.FetchIdFromMap("Fence", ClassMaps.TraderMaps));
			HandleBossRep(info, sessionId, (MongoId)_helpers.FetchIdFromMap("Fence", ClassMaps.TraderMaps));
		}
		// MoreBotsAPI 依赖已移除（2026-08-09）：Legion 自定义 boss 需要 MoreBotsAPI（com.morebotsapi.tacticaltoaster）
		// 本整合包未安装，故跳过 boss 刷怪注册与 legion 进度追踪，避免引用不存在的 bot 数据导致运行时错误。
		if (_config.EnableCustomBoss)
		{
			ROLogger.Log(_logger, "Legion custom boss requires MoreBotsAPI (com.morebotsapi.tacticaltoaster) which is not installed. Skipping Legion spawn registration.", LogTextColor.Yellow);
		}
		return new ValueTask<string>(output ?? string.Empty);
	}

	private static void HandleAssortLlItems(Dictionary<MongoId, int> assortItems)
	{
		foreach (KeyValuePair<MongoId, int> assortItem in assortItems)
		{
			assortItems[assortItem.Key] = 1;
		}
	}

	private static void HandleREStatusRep(EndLocalRaidRequestData info, MongoId sessionId, MongoId traderRepToModify)
	{
		ExitStatus? result = info.Results?.Result;
		try
		{
			if ((int)result.GetValueOrDefault() != 2 && (int)result.GetValueOrDefault() != 3 && (int)result.GetValueOrDefault() != 4 && (int)result.GetValueOrDefault() != 1)
			{
				_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.03);
				if (_debugConfig.DebugMode)
				{
					ROLogger.Log(_logger, $"Raid survived. Increasing {traderRepToModify} Rep by 0.03", LogTextColor.Cyan);
				}
			}
		}
		catch (Exception value)
		{
			ROLogger.LogError(_logger, $"Error modifying Trader Rep on Successful Raid Exfil: {value}");
		}
	}

	private static void HandleBossRep(EndLocalRaidRequestData info, MongoId sessionId, MongoId traderRepToModify)
	{
		IEnumerable<Victim> victims = info.Results?.Profile?.Stats?.Eft?.Victims ?? Array.Empty<Victim>();
		foreach (Victim victim in victims)
		{
			string text = victim.Role?.ToLower() ?? string.Empty;
			try
			{
				if (text.Contains("bosslegion"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.15);
				}
				else if (text.Contains("legionnaire"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.03);
				}
				else if (text.Contains("bossboar"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bossbully"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bossgluhar"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bosskilla"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bossknight"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bosskojaniy"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bosskolontay"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bosssanitar"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bosstagilla"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("bosszryachiy"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("followerbigpipe"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
				else if (text.Contains("followerbirdeye"))
				{
					_traderHelper.AddStandingToTrader(sessionId, traderRepToModify, 0.1);
				}
			}
			catch (Exception value)
			{
				ROLogger.LogError(_logger, $"Error modifying Trader Rep on killing boss: {value}");
			}
		}
	}
}

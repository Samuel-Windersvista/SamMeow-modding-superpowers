// ROBossHelper: SPT 4.0 -> 4.1.2 迁移
// DatabaseService.GetLocations() -> LocationTable 注入
// BossLocationSpawn 命名空间: Spt.Server -> Eft.Common
using System;
using System.Collections.Generic;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROBossHelper(ISptLogger<ROBossHelper> logger, LocationTable locationTable, RandomUtil randomUtil)
{
	private static readonly Dictionary<string, string> _mapData = new Dictionary<string, string>
	{
		["bigmap"] = "ZoneDormitory,ZoneGasStation,ZoneScavBase,ZoneBrige,ZoneCustoms,ZoneOldVill",
		["factory4_day"] = "BotZone",
		["factory4_night"] = "BotZone",
		["interchange"] = "ZoneCenterBot,ZoneCenter,ZoneOLI,ZoneIDEA,ZoneGoshan",
		["laboratory"] = "BotZoneFloor1,BotZoneFloor2,BotZoneBasement",
		["labyrinth"] = "BotZone",
		["lighthouse"] = "Zone_TreatmentContainers,Zone_Chalet,Zone_Blockpost,Zone_DestroyedHouse,Zone_Rocks,Zone_Village",
		["rezervbase"] = "ZoneRailStrorage,ZonePTOR2,ZoneBarrack,ZoneSubStorage,ZonePTOR1",
		["sandbox"] = "ZoneSandbox",
		["sandbox_high"] = "ZoneSandbox",
		["shoreline"] = "ZoneGreenHouses,ZonePort,ZoneSanatorium1,ZoneSanatorium2,ZoneSmuglers,ZoneMeteoStation",
		["tarkovstreets"] = "ZoneCarShowroom,ZoneClimova,ZoneMvd,ZoneSW01,ZoneConcordia",
		["woods"] = "ZoneWoodCutter,ZoneScavBase2,ZoneMiniHouse,ZoneBrokenVill,ZoneBigRocks"
	};

	public void SetBossSpawns(ConfigFile config, LegionProgression legionConfig, DebugFile debugConfig)
	{
		try
		{
			Dictionary<string, Location> locations = locationTable.GetDictionary();
			foreach (var (text3, zones) in _mapData)
			{
				if (locations.ContainsKey(locationTable.GetMappedKey(text3)))
				{
					List<BossLocationSpawn> bossLocationSpawn = locations[locationTable.GetMappedKey(text3)].Base.BossLocationSpawn;
					bossLocationSpawn.RemoveAll(x => x.BossName != null && x.BossName.Contains("bosslegion"));
					AddLegionSpawnsToMaps(text3, zones, bossLocationSpawn, config, legionConfig, debugConfig);
				}
			}
		}
		catch (Exception ex)
		{
			ROLogger.Log<ROBossHelper>(logger, "Error adjusting Legion spawns: " + ex.Message, LogTextColor.Red);
			throw;
		}
	}

	private void AddLegionSpawnsToMaps(string map, string zones, List<BossLocationSpawn> spawns, ConfigFile config, LegionProgression legionConfig, DebugFile debugConfig)
	{
		double num = 15.0;
		num = (!config.UseLegionGlobalSpawnChance) ? legionConfig.LegionChance : config.GlobalSpawnChance;
		BossLocationSpawn val = new BossLocationSpawn
		{
			BossChance = num,
			BossDifficulty = "normal",
			BossEscortAmount = SetEscortCount(2, 4, randomUtil),
			BossEscortDifficulty = "normal",
			BossEscortType = "legionnaire",
			BossName = "bosslegion",
			IsBossPlayer = false,
			BossZone = zones,
			ForceSpawn = true,
			IgnoreMaxBots = true,
			IsRandomTimeSpawn = false,
			SpawnMode = new string[2] { "regular", "pve" },
			Supports = null,
			Time = -1.0,
			TriggerId = "",
			TriggerName = ""
		};
		spawns.Add(val);
		if (debugConfig.DebugMode)
		{
			ROLogger.LogInfo<ROBossHelper>(logger, "Added Legion spawns to " + map + ".");
		}
	}

	private static string SetEscortCount(int min, int max, RandomUtil randomUtil)
	{
		return (randomUtil.GetInt(min, max, false) - 1).ToString();
	}
}

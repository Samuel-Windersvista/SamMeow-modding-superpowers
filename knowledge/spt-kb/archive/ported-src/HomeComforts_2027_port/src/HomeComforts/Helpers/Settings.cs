using System.Collections.Generic;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;

namespace HomeComforts.Helpers;

public class Settings
{
	public static ConfigEntry<float> ExfilSizeMultiplier;

	public static ConfigEntry<bool> AlwaysInfilAtSafehouse;

	public static ConfigEntry<bool> ScavsCanUseSafehouse;

	public static ConfigEntry<float> SpaceHeaterAOESizeMultiplier;

	public static ConfigEntry<float> SpaceHeaterHydrationBuff;

	public static ConfigEntry<float> SpaceHeaterEnergyBuff;

	public static ConfigEntry<int> CustomsSafehouseLimit;

	public static ConfigEntry<int> FactorySafehouseLimit;

	public static ConfigEntry<int> InterchangeSafehouseLimit;

	public static ConfigEntry<int> LabSafehouseLimit;

	public static ConfigEntry<int> LighthouseSafehouseLimit;

	public static ConfigEntry<int> ReserveSafehouseLimit;

	public static ConfigEntry<int> GroundZeroSafehouseLimit;

	public static ConfigEntry<int> ShorelineSafehouseLimit;

	public static ConfigEntry<int> StreetsSafehouseLimit;

	public static ConfigEntry<int> WoodsSafehouseLimit;

	private const string _safehouseLimitSectionName = "9: Number of Safehouses per Map";

	private const string _safehouseLimitSectionDescription = "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.";

	private static Dictionary<string, ConfigEntry<int>> _safehouseLimitLookup = new Dictionary<string, ConfigEntry<int>>();

	public static int ThisMapSafehouseLimit => _safehouseLimitLookup[Singleton<GameWorld>.Instance.LocationId.ToLower()].Value;

	public static void Init(ConfigFile config)
	{
		ExfilSizeMultiplier = config.Bind<float>("0: Advanced", "Exfil Area Size Multiplier", 8f, new ConfigDescription("Size of exfil trigger.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				IsAdvanced = true
			}
		}));
		AlwaysInfilAtSafehouse = config.Bind<bool>("1: Safehouse", "Always Infil at Safehouse", false, "true = always infil at the last enabled safehouse you exfil'd at. false = only infil at a safehouse if you exfil'd at it in the last raid you played on that map.");
		ScavsCanUseSafehouse = config.Bind<bool>("1: Safehouse", "Player Scavs can use Safehouse Marker Radio", false, "If safehouses can be used while on a scav raid.");
		SpaceHeaterAOESizeMultiplier = config.Bind<float>("2: Space Heater", "Space Heater AOE Size Multiplier", 14f, "Size multiplier for Space Heater area of affect zone. Requires raid restart to fully take affect.");
		SpaceHeaterHydrationBuff = config.Bind<float>("2: Space Heater", "Hydration Buff", 3.5f, "Hydration buff (per minute) while near a space heater.");
		SpaceHeaterEnergyBuff = config.Bind<float>("2: Space Heater", "Energy Buff", 3.5f, "Energy buff (per minute) while near a space heater.");
		CustomsSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Customs", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		FactorySafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Factory", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		InterchangeSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Interchange", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		LabSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Lab", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		LighthouseSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Lighthouse", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		ReserveSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Reserve", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		GroundZeroSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Ground Zero", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		ShorelineSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Shoreline", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		StreetsSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Streets", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		WoodsSafehouseLimit = config.Bind<int>("9: Number of Safehouses per Map", "Woods", 1, "Maximum number of safehouses allowed to be placed on a map. It is HIGHLY recommended to leave this number set to something quite small to avoid balance issues.");
		_safehouseLimitLookup.Add("bigmap", CustomsSafehouseLimit);
		_safehouseLimitLookup.Add("factory4_day", FactorySafehouseLimit);
		_safehouseLimitLookup.Add("factory4_night", FactorySafehouseLimit);
		_safehouseLimitLookup.Add("interchange", InterchangeSafehouseLimit);
		_safehouseLimitLookup.Add("laboratory", LabSafehouseLimit);
		_safehouseLimitLookup.Add("lighthouse", LighthouseSafehouseLimit);
		_safehouseLimitLookup.Add("rezervbase", ReserveSafehouseLimit);
		_safehouseLimitLookup.Add("sandbox", GroundZeroSafehouseLimit);
		_safehouseLimitLookup.Add("sandbox_high", GroundZeroSafehouseLimit);
		_safehouseLimitLookup.Add("shoreline", ShorelineSafehouseLimit);
		_safehouseLimitLookup.Add("tarkovstreets", StreetsSafehouseLimit);
		_safehouseLimitLookup.Add("woods", WoodsSafehouseLimit);
	}
}

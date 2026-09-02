using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using pitTeam.Server.Models;

namespace pitTeam.Server.Services;

[Injectable(InjectionType.Singleton)]
public class FriendlyServerSettingsService(ISptLogger<FriendlyServerSettingsService> logger, LostOnDeathConfig lostOnDeathConfig, PmcConfig pmcConfig)
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true,
		WriteIndented = true
	};

	public FriendlyServerSettingsRequest LoadSettings()
	{
		try
		{
			string settingsPath = GetSettingsPath();
			if (!File.Exists(settingsPath))
			{
				return new FriendlyServerSettingsRequest();
			}
			string json = File.ReadAllText(settingsPath);
			return JsonSerializer.Deserialize<FriendlyServerSettingsRequest>(json, JsonOptions) ?? new FriendlyServerSettingsRequest();
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to load pitFireTeam server settings: " + ex.Message);
			return new FriendlyServerSettingsRequest();
		}
	}

	public void SaveAndApply(FriendlyServerSettingsRequest settings)
	{
		if (settings == null)
		{
			settings = new FriendlyServerSettingsRequest();
		}
		SaveSettings(settings);
		ApplySettings(settings);
	}

	public void ApplyPersistedSettings()
	{
		ApplySettings(LoadSettings());
	}

	public FriendlyLostOnDeathSettingsResponse GetLostOnDeathSettings()
	{
		LostEquipment equipment = lostOnDeathConfig.Equipment;
		bool playerGearProtectedByRaidStatusOverride = IsPlayerGearProtectedBySvmRaidStatusOverride();
		return new FriendlyLostOnDeathSettingsResponse
		{
			PlayerGearProtectedByRaidStatusOverride = playerGearProtectedByRaidStatusOverride,
			Equipment = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
			{
				["ArmBand"] = equipment.ArmBand,
				["Compass"] = equipment.Compass,
				["Headwear"] = equipment.Headwear,
				["Earpiece"] = equipment.Earpiece,
				["FaceCover"] = equipment.FaceCover,
				["ArmorVest"] = equipment.ArmorVest,
				["Eyewear"] = equipment.Eyewear,
				["TacticalVest"] = equipment.TacticalVest,
				["PocketItems"] = equipment.PocketItems,
				["Pockets"] = equipment.PocketItems,
				["Backpack"] = equipment.Backpack,
				["Holster"] = equipment.Holster,
				["FirstPrimaryWeapon"] = equipment.FirstPrimaryWeapon,
				["SecondPrimaryWeapon"] = equipment.SecondPrimaryWeapon,
				["Scabbard"] = equipment.Scabbard,
				["SecuredContainer"] = equipment.SecuredContainer
			}
		};
	}

	private bool IsPlayerGearProtectedBySvmRaidStatusOverride()
	{
		try
		{
			string modsDir = Path.Combine(AppContext.BaseDirectory, "user", "mods");
			if (!Directory.Exists(modsDir))
			{
				return false;
			}
			foreach (string svmDir in Directory.EnumerateDirectories(modsDir, "*SVM*"))
			{
				string loaderJsonPath = Path.Combine(svmDir, "Loader", "loader.json");
				string presetsDir = Path.Combine(svmDir, "Presets");
				if (File.Exists(loaderJsonPath) && Directory.Exists(presetsDir) && IsSvmPresetProtectingPlayerDeathGear(loaderJsonPath, presetsDir, out string presetName, out string reason))
				{
					logger.Info($"Detected SVM player death gear protection from preset '{presetName}' ({reason}); death-escape will skip player gear recovery.");
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to inspect SVM raid-status override: " + ex.Message);
		}
		return false;
	}

	private static bool IsSvmPresetProtectingPlayerDeathGear(string loaderJsonPath, string presetsDir, out string presetName, out string reason)
	{
		presetName = string.Empty;
		reason = string.Empty;
		JsonNode? loaderRoot = JsonNode.Parse(File.ReadAllText(loaderJsonPath));
		presetName = loaderRoot?["CurrentlySelectedPreset"]?.ToString() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(presetName) || presetName.Equals("null", StringComparison.Ordinal))
		{
			return false;
		}
		string presetPath = Path.Combine(presetsDir, presetName + ".json");
		if (!File.Exists(presetPath))
		{
			return false;
		}
		JsonNode? presetRoot = JsonNode.Parse(File.ReadAllText(presetPath));
		JsonNode? raids = presetRoot?["Raids"];
		if (raids == null || !GetBool(raids, "EnableRaids"))
		{
			return false;
		}
		if (GetBool(raids, "SaveGearAfterDeath"))
		{
			reason = "SaveGearAfterDeath=true";
			return true;
		}
		int onKilledState = GetInt(raids, "OnKilledState", 1);
		if (onKilledState == 3 || onKilledState == 5)
		{
			reason = $"OnKilledState={onKilledState}";
			return true;
		}
		return false;
	}

	private static bool GetBool(JsonNode node, string propertyName)
	{
		JsonNode? value = node[propertyName];
		if (value == null)
		{
			return false;
		}
		return value.GetValue<bool>();
	}

	private static int GetInt(JsonNode node, string propertyName, int defaultValue)
	{
		JsonNode? value = node[propertyName];
		if (value == null)
		{
			return defaultValue;
		}
		return value.GetValue<int>();
	}

	private void SaveSettings(FriendlyServerSettingsRequest settings)
	{
		try
		{
			string path = GetSettingsPath();
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to save pitFireTeam server settings: " + ex.Message);
		}
	}

	private void ApplySettings(FriendlyServerSettingsRequest settings)
	{
		try
		{
			pmcConfig.ForceArmband.Enabled = settings.PmcArmbands;
			if (settings.PmcArmbands)
			{
				pmcConfig.ForceArmband.Usec = ItemTpl.ARMBAND_BLUE;
				pmcConfig.ForceArmband.Bear = ItemTpl.ARMBAND_RED;
			}
			logger.Info("pitFireTeam PMC armband enforcement: " + (settings.PmcArmbands ? "enabled" : "disabled"));
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to configure PMC armbands: " + ex.Message);
		}
	}

	private static string GetSettingsPath()
	{
		return Path.Combine(AppContext.BaseDirectory, "user", "mods", "pitFireTeam-ServerMod", "Resources", "settings.json");
	}
}

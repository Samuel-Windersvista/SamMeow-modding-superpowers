// ROHelpers: SPT 4.0 -> 4.1.2 迁移
// DatabaseService（4.0 聚合入口）在 4.1.2 移除，改为注入具体表模型：
//   GetTables().Templates.Handbook.Items -> TemplateTable.Handbook.Items
//   GetItems()/GetTables().Templates.Items -> TemplateTable.Items
// 命名空间变化：PresetHelper/ItemHelper -> Helpers.Items, HandbookHelper -> Helpers.Profile, ModHelper -> Helpers.Server
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Path = System.IO.Path;
using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROHelpers(
	ISptLogger<ROHelpers> logger,
	TemplateTable templateTable,
	LocaleService localeService,
	IReadOnlyList<SptMod> sptModsList,
	PresetHelper presetHelper,
	HandbookHelper handbookHelper,
	ItemHelper itemHelper,
	RandomUtil randomUtil,
	JsonUtil jsonUtil,
	FileUtil fileUtil,
	ModHelper modHelper)
{
	public bool CheckForMod(string modGuid)
	{
		return sptModsList.Any(m => m.ModMetadata.ModGuid == modGuid);
	}

	public static bool CheckFilePath(string path, string fileName)
	{
		return File.Exists(Path.Combine(path, fileName) + ".json");
	}

	public int GenRandomCount(int min, int max)
	{
		return randomUtil.RandInt(min, (int?)max);
	}

	public void ProfileBackup(MongoId sessionID, Assembly assembly)
	{
		string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(assembly);
		string value = DateTime.Now.Year.ToString();
		string value2 = DateTime.Now.Month.ToString();
		string value3 = DateTime.Now.Day.ToString();
		string value4 = DateTime.Now.Hour.ToString();
		string value5 = DateTime.Now.Minute.ToString();
		string[] buffer = new string[5];
		buffer[0] = absolutePathToModFolder;
		buffer[1] = "profileBackup";
		buffer[2] = (string)sessionID;
		buffer[3] = $"{value}/{value2}/{value3}";
		buffer[4] = $"{sessionID}-{value4}-{value5}.json";
		string text = Path.Combine(buffer);
		string[] buffer2 = new string[5];
		buffer2[0] = absolutePathToModFolder;
		buffer2[1] = "../";
		buffer2[2] = "../";
		buffer2[3] = "profiles";
		buffer2[4] = $"{sessionID}.json";
		string text2 = Path.Combine(buffer2);
		fileUtil.CopyFile(text2, text, true);
	}

	public HandbookItem? GetItemInHandbook(string itemId)
	{
		return templateTable.Handbook.Items.SingleOrDefault(x => x.Id == (MongoId)itemId);
	}

	public double? GetStackedItemPrice(MongoId itemTpl, IEnumerable<Item> items)
	{
		if (!itemHelper.IsOfBaseclass(itemTpl, BaseClasses.AMMO_BOX))
		{
			return handbookHelper.GetTemplatePrice(itemTpl) * 1.15;
		}
		return GetAmmoBoxPrice(items) * 1.15;
	}

	public double? GetAmmoBoxPrice(IEnumerable<Item> items)
	{
		double num = 0.0;
		foreach (Item item in items)
		{
			if (itemHelper.IsOfBaseclass(item.Template, BaseClasses.AMMO))
			{
				double num2 = num;
				double templatePrice = handbookHelper.GetTemplatePrice(item.Template);
				Upd? upd = item.Upd;
				num = num2 + templatePrice * ((upd != null) ? upd.StackObjectsCount : ((double?)null)) ?? 1.0;
			}
		}
		return num;
	}

	public void AddToCases(string[] casesToAdd, MongoId itemToAdd)
	{
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		foreach (string text in casesToAdd)
		{
			foreach (var (key, _) in items)
			{
				if (items[key].Id != (MongoId)text)
				{
					return;
				}
				TemplateItemProperties? properties = items[key].Properties;
				object? obj;
				if (properties == null)
				{
					obj = null;
				}
				else
				{
					IEnumerable<Grid>? grids = properties.Grids;
					if (grids == null)
					{
						obj = null;
					}
					else
					{
						GridProperties? properties2 = grids.First().Properties;
						if (properties2 == null)
						{
							obj = null;
						}
						else
						{
							IEnumerable<GridFilter>? filters = properties2.Filters;
							obj = (filters != null) ? filters.First().Filter : null;
						}
					}
				}
				if (obj == null)
				{
					return;
				}
				TemplateItemProperties? properties3 = items[key].Properties;
				if (properties3 == null)
				{
					continue;
				}
				IEnumerable<Grid>? grids2 = properties3.Grids;
				if (grids2 == null)
				{
					continue;
				}
				GridProperties? properties4 = grids2.First().Properties;
				if (properties4 != null)
				{
					IEnumerable<GridFilter>? filters2 = properties4.Filters;
					if (filters2 != null)
					{
						filters2.First().Filter?.Add(itemToAdd);
					}
				}
			}
		}
	}

	public void ModifyContainerSize(MongoId containerToModify, int horizontal, int vertical)
	{
		if (templateTable.Items.TryGetValue(containerToModify, out TemplateItem? value))
		{
			TemplateItemProperties? properties = value.Properties;
			Grid? val = (properties != null) ? properties.Grids?.First() : null;
			if (val != null)
			{
				val.Properties.CellsH = horizontal;
				val.Properties.CellsV = vertical;
			}
		}
	}

	public string FetchIdFromMap(string key, Dictionary<string, MongoId> map)
	{
		if (MongoId.IsValidMongoId(key))
		{
			return key;
		}
		if (map.TryGetValue(key, out MongoId value))
		{
			return ((MongoId)value).ToString();
		}
		throw new ArgumentException("'" + key + "' was not found in map.");
	}

	public T LoadConfig<T>(Assembly assembly, string dataPath, string configName)
	{
		string text = Path.Combine(modHelper.GetAbsolutePathToModFolder(assembly), dataPath);
		return modHelper.GetJsonDataFromFile<T>(text, configName);
	}

	public void WriteConfigFile<T>(T data, Assembly assembly, string dataPath, string configName)
	{
		if (data == null)
		{
			return;
		}
		string text = Path.Combine(modHelper.GetAbsolutePathToModFolder(assembly), dataPath);
		if (!Directory.Exists(text))
		{
			Directory.CreateDirectory(text);
		}
		string path = Path.Combine(text, configName);
		string value = jsonUtil.Serialize<T>(data, true);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		File.Create(path).Dispose();
		StreamWriter streamWriter = new StreamWriter(path);
		streamWriter.Write(value);
		streamWriter.Flush();
		streamWriter.Close();
	}

	public void DumpDataMaps(Assembly assembly)
	{
		string dataPath = Path.Combine("db", "devFiles", "dumpedData");
		SortedDictionary<string, MongoId> sortedDictionary = new SortedDictionary<string, MongoId>();
		SortedDictionary<string, Preset> sortedDictionary2 = new SortedDictionary<string, Preset>();
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		Dictionary<string, string> localeDb = localeService.GetLocaleDb(null);
		List<Preset>? allPresets = presetHelper.GetAllPresets();
		foreach (var (val3, val4) in items)
		{
			try
			{
				sortedDictionary.TryAdd(localeDb[(string)val4.Parent + " Name"].ToUpperInvariant().Replace(" ", "_").Replace(".", "")
					.Replace("(", "")
					.Replace(")", "") + "_" + localeDb[(string)val3 + " Name"].ToUpperInvariant().Replace(" ", "_").Replace(".", "")
					.Replace("(", "")
					.Replace(")", ""), val3);
			}
			catch (Exception ex)
			{
				ROLogger.Log<ROHelpers>(logger, $"Error adding item {val3} to item map => " + ex, LogTextColor.Yellow);
			}
		}
		if (allPresets != null)
		{
			foreach (Preset item in allPresets)
			{
				try
				{
					sortedDictionary2.TryAdd(item.Name.ToUpperInvariant().Replace(" ", "_"), item);
				}
				catch (Exception ex2)
				{
					ROLogger.Log<ROHelpers>(logger, "Error adding preset " + item.Name + " to preset map => " + ex2, LogTextColor.Yellow);
				}
			}
		}
		try
		{
			WriteConfigFile(sortedDictionary, assembly, dataPath, "dumpedItemMap.json");
			WriteConfigFile(sortedDictionary2, assembly, dataPath, "dumpedPresetMap.json");
		}
		catch (Exception ex3)
		{
			ROLogger.Log<ROHelpers>(logger, "Error writing maps => " + ex3, LogTextColor.White);
		}
	}
}

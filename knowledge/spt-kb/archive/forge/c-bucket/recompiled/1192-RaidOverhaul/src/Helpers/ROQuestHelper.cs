// ROQuestHelper: SPT 4.0 -> 4.1.2 迁移
// DatabaseService.GetTables() -> TemplateTable/LocaleTable 注入
// LazyLoad 命名空间: SPTarkov.Server.Core.Utils.Json
using System;
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils.Json;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROQuestHelper(ISptLogger<ROQuestHelper> logger, TemplateTable templateTable, LocaleTable localeTable, ImageRouter imageRouter, ModHelper modHelper, ROJsonHelper jsonHelper)
{
	public void CreateCustomQuests(Assembly assembly, string questPath)
	{
		string path = Path.Combine(modHelper.GetAbsolutePathToModFolder(assembly), questPath);
		List<Dictionary<MongoId, Quest>> questFiles = jsonHelper.LoadCombinedQuestJsons(Path.Combine(path, "quests"));
		List<string> images = Directory.GetFiles(Path.Combine(path, "pics")).ToList();
		string localesPath = Path.Combine(path, "locales");
		LoadQuestData(questFiles);
		LoadQuestLocales(localesPath);
		LoadQuestImgs(images);
	}

	private void LoadQuestData(List<Dictionary<MongoId, Quest>> questFiles)
	{
		int num = 0;
		foreach (Dictionary<MongoId, Quest> questFile in questFiles)
		{
			foreach (var (key, value) in questFile)
			{
				templateTable.Quests[key] = value;
				num++;
			}
		}
		ROLogger.LogDebug<ROQuestHelper>(logger, $"Successfully loaded {num} quests");
	}

	private void LoadQuestLocales(string localesPath)
	{
		Dictionary<string, Dictionary<string, string>> locales = jsonHelper.LoadCombinedLocaleJsons(localesPath);
		Dictionary<string, string>? value;
		Dictionary<string, string>? fallback = locales.TryGetValue("en", out value) ? value : locales.Values.FirstOrDefault();
		if (fallback == null)
		{
			return;
		}
		foreach (KeyValuePair<string, LazyLoad<GlobalLocaleDictionary>> item in localeTable.Global)
		{
			var (localeCode, val2) = item;
			val2.AddTransformer(localeData =>
			{
				if (localeData == null)
				{
					return localeData;
				}
				foreach (KeyValuePair<string, string> item2 in locales.GetValueOrDefault(localeCode, fallback))
				{
					localeData[item2.Key] = item2.Value;
				}
				return localeData;
			});
		}
	}

	private void LoadQuestImgs(List<string> images)
	{
		foreach (string image in images)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(image);
			imageRouter.AddRoute("/files/quest/icon/" + fileNameWithoutExtension, image);
		}
		ROLogger.LogDebug<ROQuestHelper>(logger, $"Loaded {images.Count} images");
	}
}

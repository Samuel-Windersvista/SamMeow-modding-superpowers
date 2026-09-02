// ROJsonHelper: SPT 4.0 -> 4.1.2 迁移
// JsonUtil 命名空间: SPTarkov.Server.Core.Utils.Json -> SPTarkov.Server.Core.Utils
using System.Collections.Generic;
using System.IO;
using Path = System.IO.Path;
using System.Linq;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Utils;

namespace RaidOverhaulMain.Helpers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROJsonHelper(ISptLogger<ROJsonHelper> logger, JsonUtil jsonUtil)
{
	public List<T> LoadCombinedJsons<T>(string directoryPath)
	{
		List<T> list = new List<T>();
		string[] array = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories)
			.Where(f => f.EndsWith(".json") || f.EndsWith(".jsonc"))
			.ToArray();
		foreach (string text in array)
		{
			T? val = jsonUtil.DeserializeFromFile<T>(text);
			if (val != null)
			{
				list.Add(val);
				ROLogger.LogDebug<ROJsonHelper>(logger, "Loaded file: " + text);
			}
		}
		return list;
	}

	public Dictionary<string, Dictionary<string, string>> LoadCombinedLocaleJsons(string directoryPath)
	{
		Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
		string[] array = Directory.GetFiles(directoryPath, "*.json").Concat(Directory.GetFiles(directoryPath, "*.jsonc")).ToArray();
		foreach (string text in array)
		{
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
			Dictionary<string, string>? dictionary2 = jsonUtil.DeserializeFromFile<Dictionary<string, string>>(text);
			if (dictionary2 != null)
			{
				dictionary[fileNameWithoutExtension] = dictionary2;
				ROLogger.LogDebug<ROJsonHelper>(logger, "Loaded locale file: " + text);
			}
		}
		return dictionary;
	}

	public List<Dictionary<MongoId, Quest>> LoadCombinedQuestJsons(string directory)
	{
		List<Dictionary<MongoId, Quest>> list = new List<Dictionary<MongoId, Quest>>();
		foreach (Dictionary<MongoId, Quest> item in LoadCombinedJsons<Dictionary<MongoId, Quest>>(directory))
		{
			if (item.Count > 0)
			{
				list.Add(item);
			}
		}
		return list;
	}
}

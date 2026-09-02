// Mod: SPT 4.0 -> 4.1.2 迁移
// 4.1.2 API 变更：
//   - IOnLoad.OnLoad() -> IOnLoad.OnLoadAsync(CancellationToken)
//   - DatabaseService（4.0 提供 GetHideout()）在 4.1.2 中移除，
//     改为直接注入 HideoutTable（表模型）访问 hideout.Production.Recipes
//   - Injectable 特性签名变化：4.1.2 为 (InjectionType, int typePriority)，无 typeOverride
//   - ISptLogger 命名空间：SPTarkov.Server.Core.Models.Utils -> SPTarkov.Common.Models.Logging
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace TarkovCraftLoader;

[Injectable(InjectionType.Scoped, 400001)]
public class Mod(ISptLogger<Mod> logger, HideoutTable hideoutTable) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		logger.Info("[ViniHNS] TarkovCraft Loader initialized.");
		List<HideoutProduction>? recipes = hideoutTable.Production.Recipes;
		if (recipes == null)
		{
			logger.Error("[ViniHNS] Recipes not found.");
			return Task.CompletedTask;
		}
		string location = Assembly.GetExecutingAssembly().Location;
		string? directoryName = Path.GetDirectoryName(location);
		if (directoryName == null)
		{
			logger.Error("[ViniHNS] Could not resolve mod directory.");
			return Task.CompletedTask;
		}
		string path = Path.Combine(directoryName, "recipes");
		if (!Directory.Exists(path))
		{
			logger.Warning("[ViniHNS] 'recipes' folder not found. Skipping.");
			return Task.CompletedTask;
		}
		string[] files = Directory.GetFiles(path, "*.json");
		int num = 0;
		JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions();
		jsonSerializerOptions.Converters.Add(new MongoIdConverter());
		foreach (string path2 in files)
		{
			try
			{
				string json = File.ReadAllText(path2);
				object? obj = JsonSerializer.Deserialize(json, recipes.GetType(), jsonSerializerOptions);
				if (obj is not IList list)
				{
					continue;
				}
				IList list2 = recipes;
				if (list2 == null)
				{
					continue;
				}
				foreach (object item in list)
				{
					list2.Add(item);
					num++;
				}
			}
			catch (Exception ex)
			{
				logger.Error("[ViniHNS] Failed parsing JSON file: " + Path.GetFileName(path2) + " -> " + ex.Message);
			}
		}
		logger.Info($"[ViniHNS] Loaded {num} custom recipes.");
		return Task.CompletedTask;
	}
}

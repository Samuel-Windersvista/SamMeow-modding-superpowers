using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace tarkovhdrework;

[Injectable(InjectionType.Scoped, TypePriority = 400000)]
public class TarkovHDRework(ISptLogger<TarkovHDRework> logger, ModHelper modHelper, TemplateTable templateTable) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
		Dictionary<string, string> jsonDataFromFile = modHelper.GetJsonDataFromFile<Dictionary<string, string>>(System.IO.Path.Combine(absolutePathToModFolder, "db"), "assetReplacements.json");
		Dictionary<string, string> jsonDataFromFile2 = modHelper.GetJsonDataFromFile<Dictionary<string, string>>(System.IO.Path.Combine(absolutePathToModFolder, "db"), "items.json");
		logger.Success("Mod loaded after database!");
		Dictionary<MongoId, TemplateItem> items = templateTable.Items;
		int num = 0;
		foreach (var (text3, text4) in jsonDataFromFile2)
		{
			if (!items.TryGetValue(text4, out var value) || !jsonDataFromFile.TryGetValue(text3, out var value2))
			{
				continue;
			}
			TemplateItemProperties? properties = value.Properties;
			if (properties == null)
			{
				continue;
			}
			Prefab? prefab = properties.Prefab;
			if (!(prefab == null))
			{
				string? path = prefab.Path;
				if (path != null)
				{
					prefab.Path = value2;
					num++;
					logger.Debug($"Updated {text3} ({text4}): {prefab.Path} -> {value2}");
				}
			}
		}
		logger.Success($"Asset replacement complete! Updated {num} item bundle paths.");
		return Task.CompletedTask;
	}
}

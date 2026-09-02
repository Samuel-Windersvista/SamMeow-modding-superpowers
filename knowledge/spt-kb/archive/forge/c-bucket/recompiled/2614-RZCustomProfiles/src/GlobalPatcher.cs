using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace RZCustomProfiles;

[Injectable(InjectionType.Scoped, TypePriority = 1100000)]
public class GlobalPatcher(ILogger<GlobalPatcher> logger, TemplateTable templateTable, TradersTable tradersTable, ConfigLoader configLoader) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		MasterConfig masterConfig = configLoader.Load<MasterConfig>("masterConfig.json", Assembly.GetExecutingAssembly());
		List<CustomisationStorage> customisationStorage = templateTable.CustomisationStorage;
		UnlockOutfits(masterConfig, customisationStorage);
		UnlockHideoutCustomizations(masterConfig, customisationStorage);
		return Task.CompletedTask;
	}

	private void UnlockOutfits(MasterConfig masterConfig, List<CustomisationStorage> storage)
	{
		if (!masterConfig.UnlockAllOutfits)
		{
			return;
		}
		Trader? valueOrDefault = tradersTable.GetValueOrDefault(Traders.RAGMAN);
		List<Suit>? list = valueOrDefault?.Suits;
		if (list == null)
		{
			logger.LogWarning("[RZCustomProfiles] Ragman suits is null : skipping outfit unlock.");
			return;
		}
		HashSet<MongoId> hashSet = storage.Select(s => s.Id).ToHashSet();
		int num = 0;
		foreach (Suit item in list)
		{
			if (hashSet.Add(item.SuiteId))
			{
				storage.Add(new CustomisationStorage
				{
					Id = item.SuiteId,
					Source = "unlockedInGame",
					Type = "suite"
				});
				num++;
			}
		}
	}

	private void UnlockHideoutCustomizations(MasterConfig masterConfig, List<CustomisationStorage> storage)
	{
		Dictionary<string, bool>? unlockHideoutCustomizations = masterConfig.UnlockHideoutCustomizations;
		if (unlockHideoutCustomizations == null || unlockHideoutCustomizations.Count <= 0)
		{
			return;
		}
		Dictionary<string, string> dictionary = unlockHideoutCustomizations
			.Where(kvp => kvp.Value && MasterConfig.HideoutCategories.TryGetValue(kvp.Key, out _))
			.ToDictionary(kvp => MasterConfig.HideoutCategories[kvp.Key].CategoryId, kvp => MasterConfig.HideoutCategories[kvp.Key].CustomisationType);
		if (dictionary.Count == 0)
		{
			return;
		}
		Dictionary<MongoId, CustomizationItem> customization = templateTable.Customization;
		HashSet<MongoId> hashSet = storage.Select(s => s.Id).ToHashSet();
		int num = 0;
		foreach (var (key, item) in customization)
		{
			if (!hashSet.Contains(key))
			{
				string? text = FindCategoryType(item, customization, dictionary);
				if (text != null)
				{
					storage.Add(new CustomisationStorage
					{
						Id = key,
						Source = "unlockedInGame",
						Type = text
					});
					num++;
				}
			}
		}
	}

	private static string? FindCategoryType(CustomizationItem item, Dictionary<MongoId, CustomizationItem> all, Dictionary<string, string> categoryTypeMap)
	{
		string parent = item.Parent;
		while (!string.IsNullOrEmpty(parent))
		{
			if (categoryTypeMap.TryGetValue(parent, out string value))
			{
				return value;
			}
			if (!all.TryGetValue((MongoId)parent, out CustomizationItem value2))
			{
				break;
			}
			parent = value2.Parent;
		}
		return null;
	}
}

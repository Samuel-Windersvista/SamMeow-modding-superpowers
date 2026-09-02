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
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace RZCustomProfiles;

[Injectable(InjectionType.Scoped, TypePriority = 1100001)]
public class ExaminedPatcher(ILogger<ExaminedPatcher> logger, TemplateTable templateTable, ConfigLoader configLoader) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		MasterConfig masterConfig = configLoader.Load<MasterConfig>("masterConfig.json", Assembly.GetExecutingAssembly());
		List<ProfileConfig> list = configLoader.LoadAll<ProfileConfig>("profiles", Assembly.GetExecutingAssembly())
			.Where(p => p.Enabled && p.AllItemsExamined)
			.ToList();
		if (list.Count == 0)
		{
			return Task.CompletedTask;
		}
		Dictionary<MongoId, TemplateItem>? dictionary = templateTable.Items;
		if (dictionary == null)
		{
			logger.LogWarning("[RZCustomProfiles] Templates.Items is null : skipping AllItemsExamined.");
			return Task.CompletedTask;
		}
		HandbookBase? val = templateTable.Handbook;
		if (val == null)
		{
			logger.LogWarning("[RZCustomProfiles] Handbook is null : skipping AllItemsExamined.");
			return Task.CompletedTask;
		}
		HashSet<string> source = val.Items.Select(i => i.Id.ToString()).ToHashSet(StringComparer.OrdinalIgnoreCase);
		HashSet<string> blacklistedTpls = new HashSet<string>(masterConfig.ExaminedBlacklist, StringComparer.OrdinalIgnoreCase);
		HashSet<string> hashSet = masterConfig.ExaminedCategoryBlacklist
			.Where(e => e.Enabled && !string.IsNullOrWhiteSpace(e.CategoryId))
			.Select(e => e.CategoryId)
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		if (hashSet.Count > 0)
		{
			foreach (HandbookItem item in val.Items)
			{
				string text = item.Id.ToString();
				if (IsDescendantOfAny(text, hashSet, dictionary))
				{
					blacklistedTpls.Add(text);
				}
			}
		}
		HashSet<string> hashSet2 = source.Where(t => !blacklistedTpls.Contains(t)).ToHashSet(StringComparer.OrdinalIgnoreCase);
		Dictionary<string, ProfileSides> profileTemplates = templateTable.Profiles;
		foreach (ProfileConfig item2 in list)
		{
			if (!profileTemplates.TryGetValue(item2.Name, out ProfileSides? value))
			{
				logger.LogWarning("[RZCustomProfiles] Profile '{Name}' not found : skipping AllItemsExamined.", item2.Name);
				continue;
			}
			TemplateSide[] array = new TemplateSide[] { value.Usec, value.Bear };
			foreach (TemplateSide obj in array)
			{
				PmcData? val2 = obj?.Character;
				if (val2 == null)
				{
					continue;
				}
				PmcData val3 = val2;
				if (val3.Encyclopedia == null)
				{
					val3.Encyclopedia = new Dictionary<MongoId, bool>();
				}
				int num2 = 0;
				foreach (string item3 in blacklistedTpls)
				{
					if (val2.Encyclopedia.Remove((MongoId)item3))
					{
						num2++;
					}
				}
				foreach (string item4 in hashSet2)
				{
					val2.Encyclopedia.TryAdd((MongoId)item4, value: true);
				}
			}
		}
		return Task.CompletedTask;
	}

	private static bool IsDescendantOfAny(string tpl, HashSet<string> targetParents, Dictionary<MongoId, TemplateItem> templates)
	{
		string text = tpl;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		while (!string.IsNullOrEmpty(text) && hashSet.Add(text))
		{
			if (targetParents.Contains(text))
			{
				return true;
			}
			if (!templates.TryGetValue((MongoId)text, out TemplateItem value))
			{
				break;
			}
			text = value.Parent.ToString();
		}
		return false;
	}
}

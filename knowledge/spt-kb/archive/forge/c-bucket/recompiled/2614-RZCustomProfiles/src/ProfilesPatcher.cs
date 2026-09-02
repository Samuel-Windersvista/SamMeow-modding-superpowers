using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FastCloner;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;

namespace RZCustomProfiles;

[Injectable(InjectionType.Scoped, TypePriority = 1100000)]
public class ProfilesPatcher(ILogger<ProfilesPatcher> logger, TemplateTable templateTable, TradersTable tradersTable, LocaleTable localeTable, LocaleService localeService, ConfigLoader configLoader, ProfilesUtilities profilesUtilities) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		MasterConfig masterConfig = configLoader.Load<MasterConfig>("masterConfig.json", Assembly.GetExecutingAssembly());
		List<ProfileConfig> list = configLoader.LoadAll<ProfileConfig>("profiles", Assembly.GetExecutingAssembly()).ToList();
		Dictionary<string, ProfileSides> profileTemplates = templateTable.Profiles;
		HashSet<string> allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (IGrouping<string, ProfileConfig> item in list.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1))
		{
			logger.LogError("[RZCustomProfiles] Profile name '{Name}' is defined {Count} times : profiles will overwrite each other. Check your profiles/ folder.", item.Key, item.Count());
		}
		if (masterConfig.UnlockJaeger && tradersTable.TryGetValue(Traders.JAEGER, out Trader? value))
		{
			value.Base.UnlockedByDefault = true;
		}
		if (masterConfig.UnlockRef && tradersTable.TryGetValue(Traders.REF, out Trader? value2))
		{
			value2.Base.UnlockedByDefault = true;
		}
		foreach (ProfileConfig item2 in list)
		{
			if (!item2.Enabled)
			{
				continue;
			}
			if (!MasterConfig.BaseProfiles.TryGetValue(item2.BaseProfile, out string value3))
			{
				logger.LogWarning("[RZCustomProfiles] Invalid BaseProfile '{Id}' for '{Name}' : skipping.", item2.BaseProfile, item2.Name);
				continue;
			}
			if (!profileTemplates.TryGetValue(value3, out ProfileSides? value4))
			{
				logger.LogWarning("[RZCustomProfiles] Base profile '{Base}' not found for '{Name}' : skipping.", value3, item2.Name);
				continue;
			}
			ProfileSides? val = FastCloner.FastCloner.DeepClone<ProfileSides>(value4);
			if (val == null)
			{
				logger.LogWarning("[RZCustomProfiles] Failed to clone '{Base}' for profile '{Name}' : skipping.", value3, item2.Name);
				continue;
			}
			foreach (string key in localeTable.Languages.Keys)
			{
				localeService.GetLocaleDb(key).TryAdd("launcher-profile_" + item2.Name, item2.Description ?? item2.Name);
			}
			val.DescriptionLocaleKey = item2.Description ?? item2.Name;
			profilesUtilities.PatchSide(val.Usec, "USEC", item2);
			profilesUtilities.PatchSide(val.Bear, "BEAR", item2);
			profileTemplates[item2.Name] = val;
			allowed.Add(item2.Name);
		}
		foreach (int enabledBaseProfile in masterConfig.EnabledBaseProfiles)
		{
			if (MasterConfig.BaseProfiles.TryGetValue(enabledBaseProfile, out string value5))
			{
				allowed.Add(value5);
				continue;
			}
			logger.LogWarning("[RZCustomProfiles] Unknown SPT profile index '{Index}' : skipped.", enabledBaseProfile);
		}
		foreach (string item3 in profileTemplates.Keys.Where(k => !allowed.Contains(k)).ToList())
		{
			profileTemplates.Remove(item3);
		}
		return Task.CompletedTask;
	}
}

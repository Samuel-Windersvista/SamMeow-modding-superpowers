using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Json;
using pitTeam.Server.Services;
using Path = System.IO.Path;

namespace pitTeam.Server;

[Injectable(InjectionType.Singleton, typePriority: 400001)]
public class PitFireTeamServerPlugin(ISptLogger<PitFireTeamServerPlugin> logger, TradersTable tradersTable, LocaleTable localeTable, FriendlyServerSettingsService settingsService, FriendlyTeammateService teammateService) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		EnsureCourierTraderRegistered();
		EnsureCourierTraderLocales();
		EnsureCourierAvatarIsServed();
		settingsService.ApplyPersistedSettings();
		teammateService.RecoverDuplicateTeammateItemsForAllProfiles();
		logger.Info("PitFireTeam loaded");
		return Task.CompletedTask;
	}

	private void EnsureCourierTraderRegistered()
	{
		try
		{
			if (!tradersTable.ContainsKey(FriendlyCourierTraderProfile.CourierTraderId))
			{
				tradersTable[FriendlyCourierTraderProfile.CourierTraderId] = FriendlyCourierTraderProfile.CreateTrader();
				logger.Info("Registered courier trader '67d3a28a3d6f4f7dbd09ed13'");
			}
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to register courier trader: " + ex.Message);
		}
	}

	private void EnsureCourierTraderLocales()
	{
		try
		{
			string traderId = "67d3a28a3d6f4f7dbd09ed13";
			foreach (KeyValuePair<string, LazyLoad<GlobalLocaleDictionary>> item in localeTable.Global)
			{
				var val2 = item.Value;
				val2.AddTransformer(delegate(GlobalLocaleDictionary? localized)
				{
					if (localized == null)
					{
						return localized;
					}
					FriendlyCourierTraderProfile.GetLocalizedIdentity(item.Key, out string nickname, out string location, out string description);
					localized[traderId + " Nickname"] = nickname;
					localized[traderId + " FirstName"] = nickname;
					localized[traderId + " FullName"] = nickname;
					localized[traderId + " Location"] = location;
					localized[traderId + " Description"] = description;
					return localized;
				});
			}
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to inject courier trader locale keys: " + ex.Message);
		}
	}

	private void EnsureCourierAvatarIsServed()
	{
		try
		{
			string baseDirectory = AppContext.BaseDirectory;
			string text = Path.Combine(baseDirectory, "user", "mods", "pitFireTeam-ServerMod", "Resources", "avatars", "courier.png");
			if (!File.Exists(text))
			{
				logger.Warning("Courier avatar source missing: " + text);
				return;
			}
			string text2 = Path.Combine(baseDirectory, "user", "sptappdata", "files", "trader", "avatar");
			Directory.CreateDirectory(text2);
			string destFileName = Path.Combine(text2, "pitfireteam-courier.png");
			File.Copy(text, destFileName, overwrite: true);
		}
		catch (Exception ex)
		{
			logger.Warning("Failed to publish courier avatar: " + ex.Message);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace RZCustomProfiles;

public static class HarmonyPatch
{
	public static ILogger<HarmonyHook>? Logger;

	public static SaveServer? SaveServer;

	public static PrestigeHelper? PrestigeHelper;

	public static TemplateTable? TemplateTable;

	public static TimeUtil? TimeUtil;

	public static List<ProfileConfig>? Configs;

	private static bool _skipPrestigeRewards;

	public static void ApplyPrestige(MongoId sessionId)
	{
		SptProfile? val = SaveServer?.GetProfile(sessionId);
		string? edition = val?.ProfileInfo?.Edition;
		if (edition == null)
		{
			return;
		}
		ProfileConfig? profileConfig = Configs?.FirstOrDefault(p => string.Equals(p.Name, edition, StringComparison.OrdinalIgnoreCase));
		int? num = profileConfig?.StartingPrestigeLevel;
		if (!num.HasValue)
		{
			return;
		}
		int valueOrDefault = num.GetValueOrDefault();
		if (valueOrDefault > 0)
		{
			_skipPrestigeRewards = profileConfig.SkipPrestigeRewards;
			for (int num2 = 1; num2 <= valueOrDefault; num2++)
			{
				PendingPrestige val2 = new PendingPrestige
				{
					PrestigeLevel = num2
				};
				PrestigeHelper.ProcessPendingPrestige(val, val, val2);
			}
			_skipPrestigeRewards = false;
		}
	}

	public static bool SkipPrestigeRewards()
	{
		return !_skipPrestigeRewards;
	}

	public static void ApplyTradersLoyalty(MongoId sessionId)
	{
		SptProfile? val = SaveServer?.GetProfile(sessionId);
		string? edition = val?.ProfileInfo?.Edition;
		if (edition == null)
		{
			return;
		}
		ProfileConfig? profileConfig = Configs?.FirstOrDefault(p => string.Equals(p.Name, edition, StringComparison.OrdinalIgnoreCase));
		if (profileConfig?.TradersLoyalty == null || profileConfig.TradersLoyalty.Count == 0)
		{
			return;
		}
		Dictionary<MongoId, TraderInfo>? dictionary = val?.CharacterData?.PmcData?.TradersInfo;
		if (dictionary == null)
		{
			return;
		}
		foreach (var (text2, traderLoyaltyConfig2) in profileConfig.TradersLoyalty)
		{
			MongoId key = new MongoId(text2);
			if (!dictionary.TryGetValue(key, out TraderInfo? value))
			{
				TraderInfo val2 = new TraderInfo
				{
					Unlocked = true,
					Disabled = false,
					LoyaltyLevel = 1
				};
				value = dictionary[key] = val2;
			}
			value.Standing = traderLoyaltyConfig2.Standing;
			value.SalesSum = traderLoyaltyConfig2.SalesSum;
		}
	}

	public static void ApplyAchievements(MongoId sessionId)
	{
		SptProfile? val = SaveServer?.GetProfile(sessionId);
		string? edition = val?.ProfileInfo?.Edition;
		if (edition == null || !(Configs?.FirstOrDefault(p => string.Equals(p.Name, edition, StringComparison.OrdinalIgnoreCase))?.UnlockAllAchievements ?? false))
		{
			return;
		}
		PmcData? val2 = val?.CharacterData?.PmcData;
		if (val2?.Achievements == null)
		{
			return;
		}
		List<Achievement> obj = TemplateTable?.Achievements ?? new List<Achievement>();
		long timeStamp = TimeUtil.GetTimeStamp();
		int num = 0;
		foreach (Achievement item in obj)
		{
			if (val2.Achievements.TryAdd(item.Id, timeStamp))
			{
				num++;
			}
		}
	}
}

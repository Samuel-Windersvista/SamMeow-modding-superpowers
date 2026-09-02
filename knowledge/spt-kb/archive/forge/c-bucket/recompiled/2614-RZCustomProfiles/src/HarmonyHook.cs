using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services.Profile;
using SPTarkov.Server.Core.Utils;

namespace RZCustomProfiles;

[Injectable(InjectionType.Scoped, TypePriority = 1100001)]
public class HarmonyHook(ILogger<HarmonyHook> logger, SaveServer saveServer, PrestigeHelper prestigeHelper, TemplateTable templateTable, TimeUtil timeUtil, ConfigLoader configLoader) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		HarmonyPatch.Logger = logger;
		HarmonyPatch.SaveServer = saveServer;
		HarmonyPatch.PrestigeHelper = prestigeHelper;
		HarmonyPatch.TemplateTable = templateTable;
		HarmonyPatch.TimeUtil = timeUtil;
		HarmonyPatch.Configs = configLoader.LoadAll<ProfileConfig>("profiles", Assembly.GetExecutingAssembly())
			.Where(p => p.Enabled)
			.ToList();
		Harmony val = new Harmony("com.rz.customprofiles");
		val.Patch(
			original: AccessTools.Method(typeof(CreateProfileService), "ResetAllTradersInProfile"),
			prefix: new HarmonyMethod(typeof(HarmonyPatch), nameof(HarmonyPatch.ApplyPrestige)));
		val.Patch(
			original: AccessTools.Method(typeof(CreateProfileService), "ResetAllTradersInProfile"),
			postfix: new HarmonyMethod(typeof(HarmonyPatch), nameof(HarmonyPatch.ApplyTradersLoyalty)));
		val.Patch(
			original: AccessTools.Method(typeof(CreateProfileService), "ResetAllTradersInProfile"),
			postfix: new HarmonyMethod(typeof(HarmonyPatch), nameof(HarmonyPatch.ApplyAchievements)));
		val.Patch(
			original: AccessTools.Method(typeof(PrestigeHelper), "AddPrestigeRewardsToProfile"),
			prefix: new HarmonyMethod(typeof(HarmonyPatch), nameof(HarmonyPatch.SkipPrestigeRewards)));
		return Task.CompletedTask;
	}
}

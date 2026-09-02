// ROMain: SPT 4.0 -> 4.1.2 迁移
// - IOnLoad.OnLoad() -> IOnLoad.OnLoadAsync(CancellationToken)
// - MoreBotsAPI 依赖移除（Overseer 决策 2026-08-09）：
//   MoreBotsAPI/FactionService/MoreBotsCustomBotTypeService 注入删除；
//   EnableCustomBoss 分支不再加载 Legion bot 数据/阵营/自定义 bot 类型，
//   仅保留 WTT locale 加载 + 明确日志提示（MoreBotsAPI 未安装时 Legion 无法生效，
//   且 4.1.2 DI 构造参数无法解析会导致服务端启动即炸，故必须删除而非 stub）
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using RaidOverhaulMain.Controllers;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using RaidOverhaulMain.Routers;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace RaidOverhaulMain;

[Injectable(InjectionType.Transient, int.MaxValue)]
public sealed class ROMain(ISptLogger<ROMain> logger, ROStaticRouter roStaticRouter, ROCustomItems roCustomItems, RODbEdits roDbEdits, ROTrader roTrader, ROHelpers helpers, WTTServerCommonLib.WTTServerCommonLib commonLib) : IOnLoad
{
	public async Task OnLoadAsync(CancellationToken cancellationToken)
	{
		Assembly assembly = Assembly.GetExecutingAssembly();
		string dataPath = Path.Combine("db", "devFiles");
		string hideoutCraftsPath = Path.Combine("db", "itemGen", "hideoutCrafts");
		ConfigFile config = helpers.LoadConfig<ConfigFile>(assembly, "config", "config.json");
		DebugFile debugConfig = helpers.LoadConfig<DebugFile>(assembly, dataPath, "debugOptions.json");
		EventsConfigFile eventsConfig = helpers.LoadConfig<EventsConfigFile>(assembly, "config", "eventWeightings.json");
		SeasonalProgression seasonsConfig = helpers.LoadConfig<SeasonalProgression>(assembly, dataPath, "seasonsProgressionFile.json");
		LegionProgression legionConfig = helpers.LoadConfig<LegionProgression>(assembly, "config", "legionProgressionFile.json");
		AmmoStackList ammoList = helpers.LoadConfig<AmmoStackList>(assembly, dataPath, "ammoStackList.json");
		if (debugConfig.DebugMode && debugConfig.DumpData)
		{
			helpers.DumpDataMaps(assembly);
		}
		roTrader.PassTraderConfigs(config, debugConfig);
		roDbEdits.PassDbConfigs(config, ammoList);
		roCustomItems.PassCustomItemConfigs(config);
		roStaticRouter.PassRouterConfigs(config, seasonsConfig, debugConfig, eventsConfig, legionConfig);
		await roCustomItems.BuildCustomItems();
		roTrader.BuildTrader();
		roDbEdits.BuildDbEdits();
		await commonLib.CustomHideoutRecipeService.CreateHideoutRecipes(assembly, hideoutCraftsPath);
		if (config.EnableCustomBoss)
		{
			// MoreBotsAPI（com.morebotsapi.tacticaltoaster）未安装：
			// Legion 自定义 boss / legionnaire bot / 阵营声望功能被禁用，其余 Raid Overhaul 功能照常。
			ROLogger.Log<ROMain>(logger, "Legion custom boss requires MoreBotsAPI (com.morebotsapi.tacticaltoaster) which is not installed. Legion boss features disabled; all other Raid Overhaul features remain active.", LogTextColor.Yellow);
			await commonLib.CustomLocaleService.CreateCustomLocales(assembly, Path.Combine("db", "locales", "bossEnabled"));
		}
		else
		{
			await commonLib.CustomLocaleService.CreateCustomLocales(assembly, Path.Combine("db", "locales", "bossDisabled"));
		}
		ROLogger.Log<ROMain>(logger, "Raid Overhaul Finished Loaded", LogTextColor.Magenta);
		await Task.CompletedTask;
	}
}

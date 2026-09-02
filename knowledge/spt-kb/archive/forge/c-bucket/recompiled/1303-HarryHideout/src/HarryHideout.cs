using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Routers;

namespace _harryHideout;

// 4.1.2：ConfigServer 移除 -> TraderConfig/RagfairConfig 直接注入；DatabaseService 拆分 ->
// HideoutTable/TemplateTable 注入；IOnLoad 签名改为 OnLoadAsync(CancellationToken)
[Injectable(InjectionType.Singleton)]
public class HarryHideout(
	ModHelper modHelper,
	ImageRouter imageRouter,
	TraderConfig traderConfig,
	RagfairConfig ragfairConfig,
	AddCustomTraderHelper addCustomTraderHelper,
	FluentTraderAssortCreator fluentTraderAssortCreator,
	HideoutTable hideoutTable,
	TemplateTable templateTable,
	CustomDynamicRouter dynamicRouter) : IOnLoad
{
	private readonly TraderConfig _traderConfig = traderConfig;

	private readonly RagfairConfig _ragfairConfig = ragfairConfig;

	public ModConfig? config;

	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
		config = modHelper.GetJsonDataFromFile<ModConfig>(absolutePathToModFolder, "config.json");
		BuildTraderAndAssort(absolutePathToModFolder);
		dynamicRouter.PassConfig(config!);
		new BuyItemPatch().Enable();
		return Task.CompletedTask;
	}

	private void BuildTraderAndAssort(string pathToMod)
	{
		ModConfig cfg = config!;
		string text = System.IO.Path.Combine(pathToMod, "data/harry.jpg");
		TraderBase jsonDataFromFile = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/base.json");
		imageRouter.AddRoute(jsonDataFromFile.Avatar!.Replace(".jpg", ""), text);
		addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, jsonDataFromFile, cfg.TraderRefreshMin, cfg.TraderRefreshMax);
		_ragfairConfig.Traders.TryAdd(jsonDataFromFile.Id, cfg.AddTraderToFlea);
		addCustomTraderHelper.AddTraderWithEmptyAssortToDb(jsonDataFromFile);
		addCustomTraderHelper.AddTraderToLocales(jsonDataFromFile, "Harry", "I'm sellin', what are you buyin'?");
		List<HideoutArea> areas = hideoutTable.Areas;
		List<MongoId> itemIds = new List<MongoId>();
		foreach (HideoutArea area in areas)
		{
			foreach (KeyValuePair<string, Stage> stageKvp in area.Stages!)
			{
				Stage stage = stageKvp.Value;
				if (stage.Requirements == null)
				{
					continue;
				}
				foreach (StageRequirement requirement in stage.Requirements)
				{
					MongoId templateId = requirement.TemplateId;
					if (!templateId.IsEmpty && !cfg.IgnoreList.Contains(requirement.TemplateId) && !itemIds.Contains(requirement.TemplateId))
					{
						itemIds.Add(requirement.TemplateId);
					}
				}
			}
		}
		// 4.1.2 Handbook 模型已扁平化（无 Items 查找），价格回退直接使用兜底逻辑
		List<MongoId> expensiveItems = new List<MongoId>
		{
			ItemTpl.BARTER_FARFORWARD_GPS_SIGNAL_AMPLIFIER_UNIT,
			ItemTpl.BARTER_ADVANCED_CURRENT_CONVERTER
		};
		Dictionary<MongoId, double> prices = templateTable.Prices;
		double price = 0.0;
		foreach (MongoId itemId in itemIds)
		{
			if (prices.TryGetValue(itemId, out double value))
			{
				price = value * cfg.ItemPriceMultiplier;
			}
			else
			{
				price = 1f * cfg.ItemPriceMultiplier;
			}
			if (expensiveItems.Contains(itemId))
			{
				price *= 10.0;
			}
			fluentTraderAssortCreator.CreateSingleAssortItem(itemId)
				.AddMoneyCost(Money.ROUBLES, (int)Math.Round(price))
				.AddLoyaltyLevel(1)
				.Export(jsonDataFromFile.Id);
		}
	}
}

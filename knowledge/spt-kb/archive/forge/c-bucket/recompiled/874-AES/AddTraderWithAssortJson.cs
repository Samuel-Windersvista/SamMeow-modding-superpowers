using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using WttCommon = WTTServerCommonLib.WTTServerCommonLib;

namespace AES_Trader;

[Injectable(InjectionType.Scoped, TypePriority = 400001)]
public class AddTraderWithAssortJson(ModHelper modHelper, ImageRouter imageRouter, TraderConfig traderConfig, RagfairConfig ragfairConfig, TimeUtil timeUtil, WttCommon wttCommon, AddCustomTraderHelper AddCustomTraderHelper) : IOnLoad
{
	public Task OnLoadAsync(CancellationToken cancellationToken)
	{
		string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
		string valueToAdd = System.IO.Path.Combine(absolutePathToModFolder, "db/Anastasia.jpg");
		string valueToAdd2 = System.IO.Path.Combine(absolutePathToModFolder, "db/Evelyn.jpg");
		string valueToAdd3 = System.IO.Path.Combine(absolutePathToModFolder, "db/Svetlana.jpg");
		TraderBase jsonDataFromFile = modHelper.GetJsonDataFromFile<TraderBase>(absolutePathToModFolder, "db/anastasiaBase.json");
		TraderBase jsonDataFromFile2 = modHelper.GetJsonDataFromFile<TraderBase>(absolutePathToModFolder, "db/evelynBase.json");
		TraderBase jsonDataFromFile3 = modHelper.GetJsonDataFromFile<TraderBase>(absolutePathToModFolder, "db/svetlanaBase.json");
		imageRouter.AddRoute(jsonDataFromFile.Avatar.Replace(".jpg", ""), valueToAdd);
		AddCustomTraderHelper.SetTraderUpdateTime(traderConfig, jsonDataFromFile, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
		imageRouter.AddRoute(jsonDataFromFile2.Avatar.Replace(".jpg", ""), valueToAdd2);
		AddCustomTraderHelper.SetTraderUpdateTime(traderConfig, jsonDataFromFile2, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
		imageRouter.AddRoute(jsonDataFromFile3.Avatar.Replace(".jpg", ""), valueToAdd3);
		AddCustomTraderHelper.SetTraderUpdateTime(traderConfig, jsonDataFromFile3, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
		ragfairConfig.Traders.TryAdd(jsonDataFromFile.Id, value: true);
		ragfairConfig.Traders.TryAdd(jsonDataFromFile2.Id, value: true);
		ragfairConfig.Traders.TryAdd(jsonDataFromFile3.Id, value: true);
		AddCustomTraderHelper.AddTraderWithEmptyAssortToDb(jsonDataFromFile);
		AddCustomTraderHelper.AddTraderWithEmptyAssortToDb(jsonDataFromFile2);
		AddCustomTraderHelper.AddTraderWithEmptyAssortToDb(jsonDataFromFile3);
		AddCustomTraderHelper.AddTraderToLocales(jsonDataFromFile, "Anastasia", "A Russian girl that provides the best weapons accessories in Tarkov. She don't care if you're BEAR or USEC.");
		AddCustomTraderHelper.AddTraderToLocales(jsonDataFromFile2, "Evelyn", "A trained American soldier who leads a small USEC group and seeks to join the High Command at the Military Base.");
		AddCustomTraderHelper.AddTraderToLocales(jsonDataFromFile3, "Svetlana", "A trained Bear that supervises the entrances and exits of one of the hundreds of Bear centers in Tarkov.");
		TraderAssort jsonDataFromFile4 = modHelper.GetJsonDataFromFile<TraderAssort>(absolutePathToModFolder, "db/anastasiaAssort.json");
		TraderAssort jsonDataFromFile5 = modHelper.GetJsonDataFromFile<TraderAssort>(absolutePathToModFolder, "db/evelynAssort.json");
		TraderAssort jsonDataFromFile6 = modHelper.GetJsonDataFromFile<TraderAssort>(absolutePathToModFolder, "db/svetlanaAssort.json");
		AddCustomTraderHelper.OverwriteTraderAssort(jsonDataFromFile.Id, jsonDataFromFile4);
		AddCustomTraderHelper.OverwriteTraderAssort(jsonDataFromFile2.Id, jsonDataFromFile5);
		AddCustomTraderHelper.OverwriteTraderAssort(jsonDataFromFile3.Id, jsonDataFromFile6);
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		wttCommon.CustomQuestService.CreateCustomQuests(executingAssembly, (string)null);
		return Task.CompletedTask;
	}
}

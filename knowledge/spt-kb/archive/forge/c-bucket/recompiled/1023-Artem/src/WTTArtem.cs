// 反编译重写：Artem 主类（4.0.3 → 4.1.2）
// 关键修改：OnLoad() → OnLoadAsync(CancellationToken)；ConfigServer 依赖移除（4.1 已删除该服务）
// 逻辑复刻自 ilspycmd 反编译的状态机：注册自定义物品/任务区/服装 + 商人 assort
using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;
using WTTServerCommonLib;
using WTTServerCommonLib.Services;

namespace Artem;

[Injectable(InjectionType.Singleton)]
public class WTTArtem(
    ISptLogger<WTTArtem> logger,
    ModHelper modHelper,
    ImageRouter imageRouter,
    TimeUtil timeUtil,
    WTTArtemHelper wttArtemHelper,
    WTTServerCommonLib.WTTServerCommonLib wttCommon)
    : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Assembly assembly = Assembly.GetExecutingAssembly();

        // 注册自定义物品 / 任务区 / 服装
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, null);
        await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly, null);
        await wttCommon.CustomClothingService.CreateCustomClothing(assembly, null);

        // 商人基础数据
        TraderBase traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
        TraderAssort assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "db/assort.json");

        wttArtemHelper.AddTraderWithEmptyAssortToDb(traderBase);
        wttArtemHelper.OverwriteTraderAssort(traderBase.Id, assort);
        wttArtemHelper.AddTraderToLocales(traderBase, traderBase.Name, "Artem - Norvinsk trader");

        // 商人图片路由（4.1: AddRoute(key=图片URL路径, value=本地文件路径)）
        string traderImagePath = System.IO.Path.Combine(pathToMod, "res", "artem.jpg");
        if (File.Exists(traderImagePath))
        {
            imageRouter.AddRoute($"/files/trader/{traderBase.Id}", traderImagePath);
        }
    }
}

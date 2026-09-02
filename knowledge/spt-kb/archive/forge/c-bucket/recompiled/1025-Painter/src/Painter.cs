// 反编译重写：Painter 主类（4.0.4 → 4.1.2）
// 关键修改：OnLoad() → OnLoadAsync(CancellationToken)；ConfigServer 移除
// 逻辑复刻自 ilspycmd 反编译状态机：注册自定义物品 + 油漆匠商人
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

namespace Painter;

[Injectable(InjectionType.Singleton)]
public class Painter(
    ISptLogger<Painter> logger,
    ModHelper modHelper,
    ImageRouter imageRouter,
    TimeUtil timeUtil,
    EpicTraderHelper traderHelper,
    WTTServerCommonLib.WTTServerCommonLib wttCommon)
    : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Assembly assembly = Assembly.GetExecutingAssembly();

        // 注册自定义物品（油漆匠卖的涂装物品）
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, null);

        // 商人基础数据
        TraderBase traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "db/base.json");
        TraderAssort assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "db/assort.json");

        traderHelper.AddTraderWithEmptyAssortToDb(traderBase);
        traderHelper.OverwriteTraderAssort(traderBase.Id, assort);
        traderHelper.AddTraderToLocales(traderBase, traderBase.Name, "Painter - Norvinsk trader");

        // 商人图片路由
        string traderImagePath = System.IO.Path.Combine(pathToMod, "res", "painter.jpg");
        if (File.Exists(traderImagePath))
        {
            imageRouter.AddRoute($"/files/trader/{traderBase.Id}", traderImagePath);
        }

        logger.Info("Painter trader loaded");
    }
}

// 反编译重写 + 4.1 适配：Badger 商人（4.0.13 → 4.1.2）
// 关键修改：ConfigServer.GetConfig<T>() 移除 → 直接注入 TraderConfig/RagfairConfig（4.1 配置模型可注入）
//           MongoId.op_Implicit → new MongoId()；ImageRouter.AddRoute 签名 4.1 一致
using System.IO;
using System.Reflection;
using EpicsAIO.Utilities;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Utils;

namespace EpicsAIO.Traders;

[Injectable(InjectionType.Singleton)]
public class Badger(
    ModHelper modHelper,
    ImageRouter imageRouter,
    TimeUtil timeUtil,
    EpicTraderHelper traderHelper,
    TraderConfig traderConfig,
    RagfairConfig ragfairConfig) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        string traderImagePath = System.IO.Path.Combine(absolutePathToModFolder, "res/badger.png");
        TraderBase traderBase = modHelper.GetJsonDataFromFile<TraderBase>(absolutePathToModFolder, "db/TraderBadger/Base.json");

        imageRouter.AddRoute(traderBase.Avatar!.Replace(".png", ""), traderImagePath);

        traderHelper.SetTraderUpdateTime(traderConfig, traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));
        ragfairConfig.Traders.TryAdd(traderBase.Id, true);
        traderHelper.AddTraderWithEmptyAssortToDb(traderBase);
        traderHelper.AddTraderToLocales(traderBase, "Badger", "An American gunsmith that was on vacation in St Petersburg when the conflict kicked off. He saw a business opportunity in it and got himself into more trouble than he bargained for. Luckily he has contacts.");

        TraderAssort assort = modHelper.GetJsonDataFromFile<TraderAssort>(absolutePathToModFolder, "db/TraderBadger/Assort.json");
        traderHelper.OverwriteTraderAssort(traderBase.Id.ToString(), assort);
        return Task.CompletedTask;
    }
}

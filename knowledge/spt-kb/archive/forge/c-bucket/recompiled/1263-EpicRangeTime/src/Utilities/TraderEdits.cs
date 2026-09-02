// 反编译复刻 + 4.1 适配：TraderEdits（4.0.13 → 4.1.2）
// 关键修改：DatabaseServer.GetTables().Traders → 注入 TradersTable（4.1 表模型）
// 逻辑：遍历所有商人 assort，把 AR15 carry handle rear sight 槽位改为 scope 槽位
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace EpicsAIO.Utilities;

[Injectable(InjectionType.Singleton)]
public class TraderEdits(TradersTable tradersTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Dictionary<MongoId, Trader> traders = (Dictionary<MongoId, Trader>)(object)tradersTable;
        foreach (Item item in traders.Values
            .Select(trader => trader.Assort)
            .Select(assort => assort.Items)
            .SelectMany(items => items.Where(item => item.Template == ItemTpl.IRONSIGHT_AR15_REAR_SIGHT_CARRY_HANDLE && item.SlotId == "mod_sight_rear")))
        {
            item.SlotId = "mod_scope";
        }
        return Task.CompletedTask;
    }
}

// 反编译重写 + 4.1 适配：BaseGameGlobalsEdits（4.0.13 → 4.1.2）
// 关键修改：DatabaseServer.GetTables().Globals → 注入 GlobalTable（4.1 表模型，ItemPresets 直接在表上）
// 逻辑：把 AR15 carry handle 预设里的 rear sight 槽位改为 scope 槽位
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace EpicsAIO.Utilities;

[Injectable(InjectionType.Singleton)]
public class BaseGameGlobalsEdits(GlobalTable globalTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Dictionary<MongoId, Preset> itemPresets = globalTable.ItemPresets;
        if (!itemPresets.TryGetValue(new MongoId("5af08cf886f774223c269184"), out var value))
        {
            return Task.CompletedTask;
        }
        foreach (Item item in value.Items.Where(item => item.SlotId == "mod_sight_rear" && item.Template == ItemTpl.IRONSIGHT_AR15_REAR_SIGHT_CARRY_HANDLE))
        {
            item.SlotId = "mod_scope";
        }
        return Task.CompletedTask;
    }
}

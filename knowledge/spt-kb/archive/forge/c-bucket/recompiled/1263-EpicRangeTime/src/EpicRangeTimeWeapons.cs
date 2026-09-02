// 反编译重写：EpicRangeTimeWeapons 主类（4.0.13 → 4.1.2）
// 关键修改：OnLoad() → OnLoadAsync(CancellationToken)；无 ConfigServer/DatabaseService 依赖
// 逻辑复刻自 ilspycmd 反编译：注册自定义物品/任务/任务区/本地化
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace EpicsAIO;

[Injectable(InjectionType.Singleton)]
public class EpicRangeTimeWeapons(WTTServerCommonLib.WTTServerCommonLib wttCommon) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, null);
        await wttCommon.CustomQuestService.CreateCustomQuests(assembly, null);
        await wttCommon.CustomQuestZoneService.CreateCustomQuestZones(assembly, null);
        await wttCommon.CustomLocaleService.CreateCustomLocales(assembly, null);
    }
}

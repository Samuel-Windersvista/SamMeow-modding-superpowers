// EukyreECOT 主类（反编译原样）：通过 WTT-ServerCommonLib 注册
// 自定义物品/语言/机器人装备/商人 assort/武器预设/成就/槽位图片
// 4.1.2 迁移：OnLoad() -> OnLoadAsync(CancellationToken)
using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using WTTServerCommonLib;

namespace EukyreECOT;

[Injectable(InjectionType.Singleton)]
public class EukyreECOT(WTTServerCommonLib.WTTServerCommonLib wttCommon) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, null);
        await wttCommon.CustomLocaleService.CreateCustomLocales(assembly, null);
        await wttCommon.CustomBotLoadoutService.CreateCustomBotLoadouts(assembly, null);
        await wttCommon.CustomAssortSchemeService.CreateCustomAssortSchemes(assembly, null);
        await wttCommon.CustomWeaponPresetService.CreateCustomWeaponPresets(assembly, null);
        await wttCommon.CustomAchievementService.CreateCustomAchievements(assembly, null);
        wttCommon.CustomSlotImageService.CreateSlotImages(assembly, null);
    }
}

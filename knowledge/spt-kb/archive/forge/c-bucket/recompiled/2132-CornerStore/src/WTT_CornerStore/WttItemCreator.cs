// WttItemCreator（4.0 → 4.1.2 重写）
// 4.1.2 变更：
//   1. IOnLoad.OnLoad() → OnLoadAsync(CancellationToken)
//   2. Injectable 特性签名变化：4.0 (InjectionType, Type, int32) → 4.1.2 (InjectionType, int typePriority)
//      （原 4.0 blob 解码：InjectionType=Transient，typeOverride=null，TypePriority=400002）
//   3. 其余 API（WTTServerCommonLib.CustomItemServiceExtended/CustomBuffService）经 WTT-ServerCommonLib 3.0.3
//      引用保持原样，无需 DatabaseService 直连
using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using WTTServerCommonLib;

namespace WTT_CornerStore;

[Injectable(InjectionType.Transient, 400002)]
public class WttItemCreator(WTTServerCommonLib.WTTServerCommonLib wttCommon, ISptLogger<WttItemCreator> logger) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, Path.Join("db", "Items", "Consumables"));
        await wttCommon.CustomBuffService.CreateCustomBuffs(assembly, Path.Join("db", "WTTBuffs"));
        logger.Success("Shelves stocked and spirits await.");
    }
}

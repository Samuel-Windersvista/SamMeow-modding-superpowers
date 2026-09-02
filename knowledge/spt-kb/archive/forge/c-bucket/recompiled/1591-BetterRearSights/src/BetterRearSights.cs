// BetterRearSights（反编译重写）：4.0.3 -> 4.1.2
// 关键修改：
//   SptLogger<BetterRearSights> -> ISptLogger<BetterRearSights>（SPTarkov.Common.Models.Logging）
//   OnLoad() -> OnLoadAsync(CancellationToken)（4.1 IOnLoad 新签名）
//   LogWithColor 颜色参数：4.0 LogTextColor(36)=cyan -> 4.1 Spectre.Console.Color.Cyan
//   [Injectable(Transient, null, int.MaxValue)] 4.0 三参 -> 4.1 两参 [Injectable(InjectionType.Transient)]
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using Spectre.Console;

namespace SPTBetterRearSights;

[Injectable(InjectionType.Transient)]
public class BetterRearSights(ISptLogger<BetterRearSights> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.LogWithColor("Successfully loaded Better Rear Sights! Good luck!", Color.Cyan);
        return Task.CompletedTask;
    }
}

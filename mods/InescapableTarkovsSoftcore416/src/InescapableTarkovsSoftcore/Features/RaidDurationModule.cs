using InescapableTarkovsSoftcore.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// G7 战局时长控制：<c>raidDuration.multiplier</c>（默认 1.0）作用于各图战局时限。
/// 字段核对（针对 SPT 4.1.6 运行时程序集 SPTarkov.Server.Core）：
/// 时限位于 <c>LocationTable.&lt;map&gt;.Base.EscapeTimeLimit</c>（<c>LocationBase.EscapeTimeLimit</c>）。
/// 4.1.6 该字段为可空 <c>double?</c>（5.0 为不可空 double），故 null / ≤0 一律跳过、不计数；
/// 服务器自身 <c>RaidTimeAdjustmentService.MakeAdjustmentsToMap</c> 亦写此字段。
/// 3.11 旧包字段名同为 <c>EscapeTimeLimit</c>，故倍率语义可平移。EscapeTimeLimitCoop/PVE 服务器未使用，暂不动。
/// 顺序 Order=700（D5 末位）。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class RaidDurationModule(ISptLogger<RaidDurationModule> logger)
    : FeatureModule<RaidDurationConfig>
{
    public override string Id => "raidDuration";

    public override int Order => 700;

    protected override RaidDurationConfig Section(SoftcoreConfig root) => root.RaidDuration;

    protected override ModuleReport Apply(ModContext context, RaidDurationConfig config)
    {
        var multiplier = config.Multiplier;

        if (multiplier <= 0)
        {
            var warning = $"[ITS] raidDuration: multiplier={multiplier} 非法（须 > 0），跳过";
            logger.Warning(warning);
            return ModuleReport.Ok(Id, 0, [warning]);
        }

        if (multiplier == 1.0)
        {
            return ModuleReport.Ok(Id, 0);
        }

        var changed = 0;
        foreach (var location in context.Tables.LocationTable.GetDictionary().Values)
        {
            if (location?.Base is null)
            {
                continue;
            }

            var current = location.Base.EscapeTimeLimit;
            // 4.1.6 EscapeTimeLimit 为可空 double?；null / ≤0 视为未设时限，跳过且不计数。
            if (current is not { } time || time <= 0)
            {
                continue;
            }

            location.Base.EscapeTimeLimit = time * multiplier;
            changed++;
        }

        logger.Info($"[ITS] raidDuration: 倍率 {multiplier} 已作用于 {changed} 张地图时限");
        return ModuleReport.Ok(Id, changed);
    }
}

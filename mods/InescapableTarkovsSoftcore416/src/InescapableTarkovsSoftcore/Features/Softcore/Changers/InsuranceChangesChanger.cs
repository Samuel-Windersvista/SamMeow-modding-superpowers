using InescapableTarkovsSoftcore.Config;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-C 保险：Prapor 70% 返还 / 240–360 分 / 保费 80%；Therapist 60% / 120–240 / 50%。
/// 另设保险运行间隔 10s、储存时长 30 天、不留存附件概率 50%。
/// </summary>
public sealed class InsuranceChangesChanger : ISoftcoreChanger
{
    public string Name => "insuranceChanges";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.InsuranceChanges;
        if (!options.Enabled)
        {
            return;
        }

        var insurance = context.Services.Insurance;
        if (insurance is null)
        {
            log.Warn("未注入 insuranceConfig，跳过 insuranceChanges");
            return;
        }

        if (options.PraporInsuranceChanges.Enabled)
        {
            ApplyTrader(context, insurance, Traders.PRAPOR, options.PraporInsuranceChanges, log);
        }

        if (options.TherapistInsuranceChanges.Enabled)
        {
            ApplyTrader(context, insurance, Traders.THERAPIST, options.TherapistInsuranceChanges, log);
        }

        insurance.RunIntervalSeconds = 10;
        insurance.StorageTimeOverrideSeconds = 2592000;
        log.Changed();
    }

    private static void ApplyTrader(
        SoftcoreContext context,
        InsuranceConfig insurance,
        string traderId,
        TraderInsuranceOptions options,
        SoftcoreChangeLog log)
    {
        if (!context.Tables.Traders.TryGetValue(traderId, out var trader) || trader.Base is null)
        {
            log.Warn($"未找到商人 {traderId}，跳过保险更改");
            return;
        }

        var traderInsurance = trader.Base.Insurance;
        if (traderInsurance is not null)
        {
            traderInsurance.MinReturnHour = options.ReturnTime.Min;
            traderInsurance.MaxReturnHour = options.ReturnTime.Max;
        }

        insurance.ReturnChancePercent[traderId] = options.ReturnChance;
        insurance.ChanceNoAttachmentsTakenPercent = 50;

        if (trader.Base.LoyaltyLevels is not null)
        {
            foreach (var loyaltyLevel in trader.Base.LoyaltyLevels)
            {
                loyaltyLevel.InsurancePriceCoefficient = options.InsuranceCostPercentage;
            }
        }
    }
}

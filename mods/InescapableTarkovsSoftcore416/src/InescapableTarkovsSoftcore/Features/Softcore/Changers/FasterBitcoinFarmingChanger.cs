using SPTarkov.Server.Core.Models.Enums;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-B 比特币农场：比特币配方时间 = round(原时间 / baseBitcoinTimeMultiplier)，
/// 设置 GPU 效率 gpuBoostRate（SURV：1.3 / 1.0）；可选把比特币手册价改回 10 万。
/// </summary>
public sealed class FasterBitcoinFarmingChanger : ISoftcoreChanger
{
    public const long BitcoinPriceTo100k = 100_000;

    public string Name => "fasterBitcoinFarming";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.FasterBitcoinFarming;
        if (!options.Enabled)
        {
            return;
        }

        if (SoftcoreTime.TryMultiplier(options.BaseBitcoinTimeMultiplier, "baseBitcoinTimeMultiplier", log))
        {
            var recipes = context.Tables.Hideout.Production?.Recipes;
            if (recipes is not null)
            {
                foreach (var recipe in recipes.Where(recipe => recipe.EndProduct == ItemTpl.BARTER_PHYSICAL_BITCOIN))
                {
                    // 4.1.6 ProductionTime 为可空 double?；null 跳过、不计数（不虚增变更数）。
                    if (recipe.ProductionTime is { } time)
                    {
                        recipe.ProductionTime = SoftcoreTime.ScaleRound(time, options.BaseBitcoinTimeMultiplier);
                        log.Changed();
                    }
                }
            }

            if (context.Tables.Hideout.Settings is not null)
            {
                context.Tables.Hideout.Settings.GpuBoostRate = options.GpuEfficiency;
                log.Changed();
            }
        }

        if (options.SetBitcoinPriceTo100k)
        {
            ApplyBitcoinPrice(context, log);
        }
    }

    private static void ApplyBitcoinPrice(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var handbookItems = context.Tables.Templates.Handbook?.Items;
        if (handbookItems is null)
        {
            log.Warn("未找到 handbook.items，跳过 setBitcoinPriceTo100k");
            return;
        }

        var bitcoin = handbookItems.FirstOrDefault(item => item.Id == ItemTpl.BARTER_PHYSICAL_BITCOIN);
        if (bitcoin is null)
        {
            log.Warn("handbook 中未找到比特币，跳过 setBitcoinPriceTo100k");
            return;
        }

        bitcoin.Price = BitcoinPriceTo100k;
        log.Changed();
    }
}

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>G6-B 燃料消耗：hideout.settings.generatorFuelFlowRate 乘以倍率（SURV = 4）。</summary>
public sealed class FuelConsumptionChanger : ISoftcoreChanger
{
    public string Name => "fuelConsumption";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.FuelConsumption;
        if (!options.Enabled)
        {
            return;
        }

        var multiplier = options.FuelConsumptionMultiplier;
        if (!SoftcoreTime.TryMultiplier(multiplier, "fuelConsumptionMultiplier", log))
        {
            return;
        }

        var settings = context.Tables.Hideout.Settings;
        if (settings?.GeneratorFuelFlowRate is null)
        {
            log.Warn("未找到 hideout.settings.generatorFuelFlowRate，跳过");
            return;
        }

        settings.GeneratorFuelFlowRate *= multiplier;
        log.Changed();
    }
}

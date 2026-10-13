namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-B 健身：严重肌肉疼痛下允许健身训练，效率置为 0.75
/// （源 TS：globals.config.Health.Effects.SevereMusclePain.GymEffectivity = 0.75）。
/// </summary>
public sealed class GymTrainingChanger : ISoftcoreChanger
{
    public const double MusclePainGymEffectivity = 0.75;

    public string Name => "allowGymTrainingWithMusclePain";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        if (!context.Config.AllowGymTrainingWithMusclePain)
        {
            return;
        }

        var pain = context.Tables.Global?.Configuration?.Health?.Effects?.SevereMusclePain;
        if (pain is null)
        {
            log.Warn("未找到 globals.config.Health.Effects.SevereMusclePain，跳过");
            return;
        }

        if (pain.GymEffectivity.Equals(MusclePainGymEffectivity))
        {
            return;
        }

        pain.GymEffectivity = MusclePainGymEffectivity;
        log.Changed();
    }
}

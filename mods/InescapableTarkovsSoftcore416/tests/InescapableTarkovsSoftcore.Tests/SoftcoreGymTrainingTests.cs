using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

public class SoftcoreGymTrainingTests
{
    [Fact]
    public void GymTraining_SetsMusclePainEffectivityTo75Percent()
    {
        var global = SoftcoreTestData.NewGlobalTable(gymEffectivity: 0.25);
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), global: global);

        new GymTrainingChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(0.75, global.Configuration.Health.Effects.SevereMusclePain.GymEffectivity);
    }

    [Fact]
    public void GymTraining_Disabled_LeavesEffectivityUnchanged()
    {
        var global = SoftcoreTestData.NewGlobalTable(gymEffectivity: 0.25);
        var config = new Config.SoftcoreModuleConfig { AllowGymTrainingWithMusclePain = false };
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), config, global: global);

        var log = new SoftcoreChangeLog();
        new GymTrainingChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(0.25, global.Configuration.Health.Effects.SevereMusclePain.GymEffectivity);
    }

    [Fact]
    public void GymTraining_MissingGlobal_WarnsAndZeroChanges()
    {
        var context = SoftcoreTestData.NewContext(
            SoftcoreTestData.NewTemplates(), SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig());

        var log = new SoftcoreChangeLog();
        new GymTrainingChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Contains(log.Warnings, warning => warning.Contains("SevereMusclePain", StringComparison.Ordinal));
    }
}

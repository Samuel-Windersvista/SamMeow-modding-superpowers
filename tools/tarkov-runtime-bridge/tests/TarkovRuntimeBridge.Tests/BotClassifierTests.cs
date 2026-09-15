using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class BotClassifierTests
{
    [Theory]
    [InlineData("bossKilla")]
    [InlineData("bossBully")]
    [InlineData("BOSS")]
    [InlineData("pmcBoss")]
    public void Role_containing_boss_is_boss(string role)
    {
        Assert.True(BotClassifier.IsBoss(role));
        Assert.Equal(BotClassifier.Boss, BotClassifier.Classify(role, BotClassifier.SavageSide));
    }

    [Theory]
    [InlineData("sectantPriest")]
    [InlineData("SectantPriest")]
    [InlineData("sectantpriest")]
    [InlineData("SECTANTPRIEST")]
    public void Explicit_boss_role_is_boss_case_insensitively(string role)
    {
        Assert.True(BotClassifier.IsBoss(role));
        Assert.Equal(BotClassifier.Boss, BotClassifier.Classify(role, BotClassifier.SavageSide));
    }

    [Theory]
    [InlineData("pmcUSEC")]
    [InlineData("pmcBEAR")]
    [InlineData("PMCBot")]
    public void Pmc_role_prefix_is_pmc(string role)
    {
        Assert.False(BotClassifier.IsBoss(role));
        Assert.Equal(BotClassifier.Pmc, BotClassifier.Classify(role, BotClassifier.SavageSide));
    }

    [Fact]
    public void Savage_side_without_pmc_role_is_scav()
    {
        Assert.Equal(BotClassifier.Scav, BotClassifier.Classify("assault", BotClassifier.SavageSide));
    }

    [Fact]
    public void Non_savage_side_is_other()
    {
        Assert.Equal(BotClassifier.Other, BotClassifier.Classify("assault", "Usec"));
        Assert.Equal(BotClassifier.Other, BotClassifier.Classify("assault", "Bear"));
    }

    [Fact]
    public void Empty_role_falls_back_to_side()
    {
        Assert.Equal(BotClassifier.Scav, BotClassifier.Classify(null, BotClassifier.SavageSide));
        Assert.Equal(BotClassifier.Scav, BotClassifier.Classify(string.Empty, BotClassifier.SavageSide));
        Assert.Equal(BotClassifier.Other, BotClassifier.Classify(null, "Usec"));
        Assert.Equal(BotClassifier.Other, BotClassifier.Classify(null, null));
    }

    [Fact]
    public void Boss_role_takes_priority_over_pmc_prefix()
    {
        Assert.Equal(BotClassifier.Boss, BotClassifier.Classify("pmcBoss", BotClassifier.SavageSide));
    }

    [Fact]
    public void Empty_role_is_not_boss()
    {
        Assert.False(BotClassifier.IsBoss(null));
        Assert.False(BotClassifier.IsBoss(string.Empty));
    }
}

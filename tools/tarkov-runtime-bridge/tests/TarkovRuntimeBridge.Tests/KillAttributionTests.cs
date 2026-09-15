using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class KillAttributionTests
{
    private static readonly KillerInfo Attacker =
        new KillerInfo("attacker-1", "SamMeow", "Usec", "pmcUSEC", true);

    [Fact]
    public void Killer_is_attributed_within_window()
    {
        var attribution = new KillAttribution(windowMs: 10_000);
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_000, Attacker);

        var found = attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 5_000, out var killer);

        Assert.True(found);
        Assert.Equal("attacker-1", killer.ProfileId);
        Assert.Equal("SamMeow", killer.Name);
        Assert.Equal("Usec", killer.Side);
        Assert.Equal("pmcUSEC", killer.Role);
        Assert.True(killer.IsLocal);
    }

    [Fact]
    public void Killer_is_not_attributed_after_window()
    {
        var attribution = new KillAttribution(windowMs: 10_000);
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_000, Attacker);

        Assert.False(attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 11_001, out _));
    }

    [Fact]
    public void Killer_is_not_attributed_across_raids()
    {
        var attribution = new KillAttribution();
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_000, Attacker);

        Assert.False(attribution.TryTakeKiller("raid-2", "victim-1", nowMs: 1_100, out _));
    }

    [Fact]
    public void Unknown_victim_is_not_attributed()
    {
        var attribution = new KillAttribution();

        Assert.False(attribution.TryTakeKiller("raid-1", "nobody", nowMs: 1_000, out _));
    }

    [Fact]
    public void Empty_victim_id_is_ignored()
    {
        var attribution = new KillAttribution();
        attribution.RecordDamage("raid-1", string.Empty, nowMs: 1_000, Attacker);

        Assert.False(attribution.TryTakeKiller("raid-1", string.Empty, nowMs: 1_000, out _));
    }

    [Fact]
    public void Killer_is_consumed_on_first_take()
    {
        var attribution = new KillAttribution();
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_000, Attacker);

        Assert.True(attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 1_100, out _));
        Assert.False(attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 1_200, out _));
    }

    [Fact]
    public void Latest_damager_wins()
    {
        var attribution = new KillAttribution();
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_000, Attacker);
        var second = new KillerInfo("attacker-2", "Other", "Bear", "pmcBEAR", false);
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_500, second);

        Assert.True(attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 1_600, out var killer));
        Assert.Equal("attacker-2", killer.ProfileId);
    }

    [Fact]
    public void Clear_drops_all_records()
    {
        var attribution = new KillAttribution();
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_000, Attacker);

        attribution.Clear();

        Assert.False(attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 1_100, out _));
    }

    [Fact]
    public void Negative_window_clamps_to_zero()
    {
        var attribution = new KillAttribution(windowMs: -1);
        attribution.RecordDamage("raid-1", "victim-1", nowMs: 1_000, Attacker);

        Assert.True(attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 1_000, out _));
        Assert.False(attribution.TryTakeKiller("raid-1", "victim-1", nowMs: 1_001, out _));
    }
}

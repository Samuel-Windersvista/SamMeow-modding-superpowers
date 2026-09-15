using System;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class RaidEventBufferTests
{
    private static readonly long Ticks =
        new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc).Ticks;

    private static long AppendDamage(RaidEventBuffer buffer, string raidId, float amount)
    {
        return buffer.AppendDamage(Ticks, raidId, new DamagePayload("victim", false, "Chest", amount, "Bullet"));
    }

    // ---------- seq ----------

    [Fact]
    public void Seq_starts_at_one_and_is_monotonic()
    {
        var buffer = new RaidEventBuffer();

        Assert.Equal(1, AppendDamage(buffer, "raid-1", 1f));
        Assert.Equal(2, AppendDamage(buffer, "raid-1", 2f));
        Assert.Equal(3, AppendDamage(buffer, "raid-1", 3f));
        Assert.Equal(3, buffer.LatestSeq);
    }

    [Fact]
    public void Events_carry_kind_raid_id_and_timestamp()
    {
        var buffer = new RaidEventBuffer();
        buffer.AppendDeath(
            Ticks,
            "raid-7",
            new DeathPayload("v", true, "Bullet", false, default));

        var page = buffer.Snapshot(0, 100);

        Assert.Single(page.Events);
        Assert.Equal(RaidEventKind.Death, page.Events[0].Kind);
        Assert.Equal("raid-7", page.Events[0].RaidId);
        Assert.Equal(Ticks, page.Events[0].TimestampUtcTicks);
    }

    // ---------- snapshot / since ----------

    [Fact]
    public void Snapshot_returns_only_events_newer_than_since()
    {
        var buffer = new RaidEventBuffer();
        AppendDamage(buffer, "raid-1", 1f);
        AppendDamage(buffer, "raid-1", 2f);
        AppendDamage(buffer, "raid-1", 3f);

        var page = buffer.Snapshot(1, 100);

        Assert.Equal(3, page.Seq);
        Assert.Equal(0, page.Dropped);
        Assert.Equal(2, page.Events.Length);
        Assert.Equal(2, page.Events[0].Seq);
        Assert.Equal(3, page.Events[1].Seq);
    }

    [Fact]
    public void Snapshot_limit_truncates_but_seq_still_reports_latest()
    {
        var buffer = new RaidEventBuffer();
        AppendDamage(buffer, "raid-1", 1f);
        AppendDamage(buffer, "raid-1", 2f);
        AppendDamage(buffer, "raid-1", 3f);

        var page = buffer.Snapshot(0, 2);

        Assert.Equal(3, page.Seq);
        Assert.Equal(2, page.Events.Length);
        Assert.Equal(1, page.Events[0].Seq);
        Assert.Equal(2, page.Events[1].Seq);
    }

    [Fact]
    public void Snapshot_limit_defaults_when_non_positive()
    {
        var buffer = new RaidEventBuffer();
        AppendDamage(buffer, "raid-1", 1f);

        Assert.Single(buffer.Snapshot(0, 0).Events);
        Assert.Single(buffer.Snapshot(0, -5).Events);
    }

    [Fact]
    public void Negative_since_is_treated_as_zero()
    {
        var buffer = new RaidEventBuffer();
        AppendDamage(buffer, "raid-1", 1f);

        var page = buffer.Snapshot(-10, 100);

        Assert.Equal(0, page.Dropped);
        Assert.Single(page.Events);
    }

    // ---------- ring eviction / dropped ----------

    [Fact]
    public void Ring_keeps_capacity_and_evicts_oldest()
    {
        var buffer = new RaidEventBuffer(capacity: 3);
        for (var i = 1; i <= 5; i++)
        {
            AppendDamage(buffer, "raid-1", i);
        }

        var page = buffer.Snapshot(0, 100);

        Assert.Equal(5, page.Seq);
        Assert.Equal(3, page.Events.Length);
        Assert.Equal(3, page.Events[0].Seq);
        Assert.Equal(4, page.Events[1].Seq);
        Assert.Equal(5, page.Events[2].Seq);
    }

    [Fact]
    public void Since_older_than_buffer_reports_dropped_and_starts_at_oldest()
    {
        var buffer = new RaidEventBuffer(capacity: 3);
        for (var i = 1; i <= 5; i++)
        {
            AppendDamage(buffer, "raid-1", i);
        }

        var page = buffer.Snapshot(0, 100);

        Assert.Equal(2, page.Dropped);
        Assert.Equal(3, page.Events[0].Seq);
    }

    [Fact]
    public void Since_at_oldest_boundary_reports_no_drop()
    {
        var buffer = new RaidEventBuffer(capacity: 3);
        for (var i = 1; i <= 5; i++)
        {
            AppendDamage(buffer, "raid-1", i);
        }

        var page = buffer.Snapshot(2, 100);

        Assert.Equal(0, page.Dropped);
        Assert.Equal(3, page.Events[0].Seq);
    }

    [Fact]
    public void Buffer_survives_raid_change_and_keeps_raid_ids()
    {
        var buffer = new RaidEventBuffer();
        AppendDamage(buffer, "raid-1", 1f);
        AppendDamage(buffer, "raid-2", 2f);

        var page = buffer.Snapshot(0, 100);

        Assert.Equal(2, page.Events.Length);
        Assert.Equal("raid-1", page.Events[0].RaidId);
        Assert.Equal("raid-2", page.Events[1].RaidId);
    }

    [Fact]
    public void Capacity_is_clamped_to_at_least_one()
    {
        var buffer = new RaidEventBuffer(capacity: 0);

        Assert.Equal(1, buffer.Capacity);
        AppendDamage(buffer, "raid-1", 1f);
        AppendDamage(buffer, "raid-1", 2f);

        var page = buffer.Snapshot(0, 100);
        Assert.Single(page.Events);
        Assert.Equal(2, page.Events[0].Seq);
    }
}

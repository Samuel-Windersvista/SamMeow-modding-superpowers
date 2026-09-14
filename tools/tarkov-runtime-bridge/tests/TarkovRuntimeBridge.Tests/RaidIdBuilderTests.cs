using System;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class RaidIdBuilderTests
{
    private static readonly long KnownTicks =
        new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc).Ticks;

    // ---------- profileId fallback chain ----------

    [Fact]
    public void Profile_id_prefers_main_player()
    {
        Assert.Equal("main-player", RaidIdBuilder.ResolveProfileId("main-player", "current-profile"));
    }

    [Fact]
    public void Profile_id_falls_back_to_current_profile()
    {
        Assert.Equal("current-profile", RaidIdBuilder.ResolveProfileId("", "current-profile"));
        Assert.Equal("current-profile", RaidIdBuilder.ResolveProfileId(null, "current-profile"));
    }

    [Fact]
    public void Profile_id_falls_back_to_unknown_profile_marker()
    {
        Assert.Equal("unknown-profile", RaidIdBuilder.ResolveProfileId("", ""));
        Assert.Equal("unknown-profile", RaidIdBuilder.ResolveProfileId(null, null));
    }

    [Fact]
    public void Profile_id_marker_constant_is_stable()
    {
        Assert.Equal("unknown-profile", RaidIdBuilder.UnknownProfileMarker);
    }

    // ---------- session start formatting ----------

    [Fact]
    public void Missing_session_start_formats_as_no_start()
    {
        Assert.Equal("no-start", RaidIdBuilder.FormatSessionStart(0));
        Assert.Equal("no-start", RaidIdBuilder.FormatSessionStart(-1));
    }

    [Fact]
    public void Session_start_marker_constant_is_stable()
    {
        Assert.Equal("no-start", RaidIdBuilder.NoStartMarker);
    }

    [Fact]
    public void Session_start_formats_as_utc_iso_8601()
    {
        Assert.Equal("2026-09-14T00:00:00.0000000Z", RaidIdBuilder.FormatSessionStart(KnownTicks));
    }

    // ---------- raidId assembly ----------

    [Fact]
    public void Raid_id_without_session_start_uses_no_start_marker()
    {
        Assert.Equal("profile-1@no-start", RaidIdBuilder.Build("profile-1", 0));
    }

    [Fact]
    public void Raid_id_with_unknown_profile_and_no_start_keeps_both_markers()
    {
        Assert.Equal(
            "unknown-profile@no-start",
            RaidIdBuilder.Build(RaidIdBuilder.UnknownProfileMarker, 0));
    }

    [Fact]
    public void Raid_id_joins_profile_and_session_start()
    {
        var raidId = RaidIdBuilder.Build("profile-1", KnownTicks);

        Assert.Equal("profile-1@2026-09-14T00:00:00.0000000Z", raidId);
    }

    [Fact]
    public void Raid_id_session_start_is_parseable_utc()
    {
        var raidId = RaidIdBuilder.Build("profile-1", KnownTicks);
        var timestamp = raidId.Substring(raidId.IndexOf('@') + 1);

        Assert.True(DateTime.TryParse(
            timestamp,
            null,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out var parsed));
        Assert.Equal(DateTimeKind.Utc, parsed.Kind);
        Assert.Equal(new DateTime(KnownTicks, DateTimeKind.Utc), parsed);
    }
}

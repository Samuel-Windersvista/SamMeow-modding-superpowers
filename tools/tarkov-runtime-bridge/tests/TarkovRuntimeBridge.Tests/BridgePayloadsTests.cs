using System;
using System.Globalization;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class BridgePayloadsTests
{
    // ---------- /bridge/info ----------

    [Fact]
    public void Info_payload_has_stable_field_order_and_values()
    {
        var body = BridgePayloads.BuildInfo("0.1.0", 1, 250, 49777);

        Assert.Equal(
            "{\"pluginVersion\":\"0.1.0\",\"protocolVersion\":1," +
            "\"capabilities\":{\"endpoints\":[\"/bridge/info\",\"/raid/player\",\"/raid/status\",\"/raid/bots\"]," +
            "\"sections\":[\"player\",\"raid\",\"bots\"]}," +
            "\"sampling\":{\"intervalMs\":250}," +
            "\"network\":{\"host\":\"127.0.0.1\",\"port\":49777}}",
            body);
    }

    // ---------- /raid/player ----------

    [Fact]
    public void Player_payload_matches_contract()
    {
        var state = TestStates.PlayerState();

        var body = BridgePayloads.BuildPlayer(state, nowMs: 1750);

        Assert.Equal(
            "{\"inRaid\":true," +
            "\"position\":{\"x\":1,\"y\":2,\"z\":3}," +
            "\"rotation\":{\"x\":0,\"y\":90}," +
            "\"pose\":\"Stand\"," +
            "\"health\":{\"alive\":true,\"total\":100," +
            "\"parts\":{\"Head\":35,\"Chest\":40,\"Stomach\":30,\"LeftArm\":25,\"RightArm\":25,\"LeftLeg\":30,\"RightLeg\":30}}," +
            "\"sampleAgeMs\":750}",
            body);
    }

    [Fact]
    public void Player_payload_degrades_nan_and_infinity_to_zero()
    {
        var state = TestStates.PlayerState(
            x: float.NaN,
            y: float.PositiveInfinity,
            z: float.NegativeInfinity,
            rotationX: float.NaN,
            rotationY: float.PositiveInfinity,
            total: float.NaN);

        var body = BridgePayloads.BuildPlayer(state, nowMs: 1000);

        Assert.Equal(
            "{\"inRaid\":true," +
            "\"position\":{\"x\":0,\"y\":0,\"z\":0}," +
            "\"rotation\":{\"x\":0,\"y\":0}," +
            "\"pose\":\"Stand\"," +
            "\"health\":{\"alive\":true,\"total\":0," +
            "\"parts\":{\"Head\":35,\"Chest\":40,\"Stomach\":30,\"LeftArm\":25,\"RightArm\":25,\"LeftLeg\":30,\"RightLeg\":30}}," +
            "\"sampleAgeMs\":0}",
            body);
    }

    [Fact]
    public void Player_payload_reports_dead_player()
    {
        var state = TestStates.PlayerState(alive: false, total: 0f);

        var body = BridgePayloads.BuildPlayer(state, nowMs: 1000);

        Assert.Contains("\"alive\":false", body);
        Assert.Contains("\"total\":0", body);
    }

    // ---------- /raid/status ----------

    [Fact]
    public void Status_payload_matches_contract()
    {
        var state = TestStates.PlayerState();

        var body = BridgePayloads.BuildStatus(state, nowMs: 1750);

        Assert.Equal(
            "{\"inRaid\":true," +
            "\"map\":\"factory4_day\"," +
            "\"status\":\"Started\"," +
            "\"remainingSeconds\":120.5," +
            "\"raidId\":\"profile-1@2026-09-14T00:00:00.0000000Z\"," +
            "\"sampleAgeMs\":750}",
            body);
    }

    [Fact]
    public void Status_payload_escapes_map_and_raid_id()
    {
        var state = new RaidState(
            new PlayerSnapshot(
                new Vector3Snapshot(0f, 0f, 0f),
                new Vector2Snapshot(0f, 0f),
                "Stand",
                new HealthSnapshot(true, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f)),
            new RaidMetaSnapshot("map\"x", "Started", 0f, "id\\1@no-start"),
            new BotSummary(0, 0, 0, 0, 0, 0, 0, 0, 0),
            Array.Empty<BotEntry>(),
            1000);

        var body = BridgePayloads.BuildStatus(state, nowMs: 1000);

        Assert.Contains("\"map\":\"map\\\"x\"", body);
        Assert.Contains("\"raidId\":\"id\\\\1@no-start\"", body);
    }

    // ---------- /raid/bots ----------

    [Fact]
    public void Bots_summary_payload_matches_contract()
    {
        var state = TestStates.BotsState(total: 4);

        var body = BridgePayloads.BuildBots(state, detail: false, nowMs: 1750);

        Assert.Equal(
            "{\"inRaid\":true,\"total\":4,\"alive\":4," +
            "\"byCategory\":{\"pmc\":2,\"scav\":1,\"boss\":1,\"other\":0}," +
            "\"spawner\":{\"aliveAndLoading\":3,\"delayed\":1,\"allWithDelayed\":4}," +
            "\"sampleAgeMs\":750}",
            body);
    }

    [Fact]
    public void Bots_detail_payload_includes_entries_and_truncated_flag()
    {
        var state = TestStates.BotsState(
            total: 4,
            new BotEntry(new Vector3Snapshot(1f, 2f, 3f), "pmcBot", "Savage", true),
            new BotEntry(new Vector3Snapshot(-4.5f, 0f, 0f), "bossKilla", "Savage", false));

        var body = BridgePayloads.BuildBots(state, detail: true, nowMs: 1750);

        Assert.Equal(
            "{\"inRaid\":true,\"total\":4,\"alive\":4," +
            "\"byCategory\":{\"pmc\":2,\"scav\":1,\"boss\":1,\"other\":0}," +
            "\"spawner\":{\"aliveAndLoading\":3,\"delayed\":1,\"allWithDelayed\":4}," +
            "\"bots\":[" +
            "{\"x\":1,\"y\":2,\"z\":3,\"role\":\"pmcBot\",\"side\":\"Savage\",\"alive\":true}," +
            "{\"x\":-4.5,\"y\":0,\"z\":0,\"role\":\"bossKilla\",\"side\":\"Savage\",\"alive\":false}" +
            "],\"truncated\":true,\"sampleAgeMs\":750}",
            body);
    }

    [Fact]
    public void Bots_detail_is_not_truncated_when_counts_match()
    {
        var state = TestStates.BotsState(
            total: 1,
            new BotEntry(new Vector3Snapshot(0f, 0f, 0f), "assault", "Savage", true));

        var body = BridgePayloads.BuildBots(state, detail: true, nowMs: 1000);

        Assert.Contains("\"truncated\":false", body);
    }

    [Fact]
    public void Bots_detail_with_no_entries_is_empty_array()
    {
        var state = TestStates.BotsState(total: 0);

        var body = BridgePayloads.BuildBots(state, detail: true, nowMs: 1000);

        Assert.Contains("\"bots\":[],\"truncated\":false", body);
    }

    // ---------- sampleAgeMs ----------

    [Fact]
    public void Age_is_difference_between_now_and_sample_time()
    {
        var state = TestStates.PlayerState(sampledAtMs: 1000);

        Assert.Equal(750, BridgePayloads.AgeMs(state, nowMs: 1750));
        Assert.Equal(0, BridgePayloads.AgeMs(state, nowMs: 1000));
    }

    [Fact]
    public void Age_clamps_negative_clock_skew_to_zero()
    {
        var state = TestStates.PlayerState(sampledAtMs: 5000);

        Assert.Equal(0, BridgePayloads.AgeMs(state, nowMs: 1000));
    }

    // ---------- float formatting ----------

    [Theory]
    [InlineData(1.5f, "1.5")]
    [InlineData(-2.25f, "-2.25")]
    [InlineData(0f, "0")]
    [InlineData(90f, "90")]
    [InlineData(0.1f, "0.1")]
    public void Float_format_uses_round_trip_notation(float value, string expected)
    {
        Assert.Equal(expected, BridgePayloads.FormatFloat(value));
    }

    [Fact]
    public void Float_format_degrades_nan_and_infinity_to_zero()
    {
        Assert.Equal("0", BridgePayloads.FormatFloat(float.NaN));
        Assert.Equal("0", BridgePayloads.FormatFloat(float.PositiveInfinity));
        Assert.Equal("0", BridgePayloads.FormatFloat(float.NegativeInfinity));
    }

    [Fact]
    public void Float_format_is_culture_invariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // de-DE 用 "," 作小数点；序列化必须仍是 "."。
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            Assert.Equal("1.5", BridgePayloads.FormatFloat(1.5f));
            Assert.Equal("120.5", BridgePayloads.FormatFloat(120.5f));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    // ---------- string formatting ----------

    [Theory]
    [InlineData(null, "\"\"")]
    [InlineData("", "\"\"")]
    [InlineData("Stand", "\"Stand\"")]
    [InlineData("a\"b", "\"a\\\"b\"")]
    [InlineData("a\\b", "\"a\\\\b\"")]
    [InlineData("a\nb", "\"a\\nb\"")]
    [InlineData("a\rb", "\"a\\rb\"")]
    [InlineData("a\tb", "\"a\\tb\"")]
    [InlineData("a\u0001b", "\"a\\u0001b\"")]
    [InlineData("factory4_day", "\"factory4_day\"")]
    public void String_format_escapes_json_specials(string value, string expected)
    {
        Assert.Equal(expected, BridgePayloads.FormatString(value));
    }

    [Fact]
    public void String_format_passes_unicode_through()
    {
        Assert.Equal("\"云文件\"", BridgePayloads.FormatString("云文件"));
    }

    [Fact]
    public void Bool_format_is_lowercase_literal()
    {
        Assert.Equal("true", BridgePayloads.FormatBool(true));
        Assert.Equal("false", BridgePayloads.FormatBool(false));
    }
}

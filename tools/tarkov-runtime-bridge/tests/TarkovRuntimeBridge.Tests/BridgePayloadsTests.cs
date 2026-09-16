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
            "\"capabilities\":{\"endpoints\":[\"/bridge/info\",\"/raid/player\",\"/raid/status\",\"/raid/bots\",\"/raid/events\",\"/logs/recent\",\"/logs/summary\"]," +
            "\"sections\":[\"player\",\"raid\",\"bots\",\"events\",\"logs\"]}," +
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
            "\"weapon\":null,\"equipment\":[]," +
            "\"sampleAgeMs\":750}",
            body);
    }

    [Fact]
    public void Player_payload_includes_weapon_and_equipment()
    {
        var state = TestStates.PlayerState(
            weapon: new WeaponSnapshot("5447a9cd4bdc2dbd208b4567", "M4A1", 30, 1),
            equipment: new[]
            {
                new EquipmentEntry("Headwear", "5aa7e276e5b5b000171d0647", "Altyn"),
                new EquipmentEntry("ArmorVest", "545cdb794bdc2d3a198b456a", "6B13"),
            });

        var body = BridgePayloads.BuildPlayer(state, nowMs: 1000);

        Assert.Contains(
            "\"weapon\":{\"tpl\":\"5447a9cd4bdc2dbd208b4567\",\"name\":\"M4A1\"," +
            "\"ammoInMag\":30,\"ammoInChamber\":1}",
            body);
        Assert.Contains(
            "\"equipment\":[" +
            "{\"slot\":\"Headwear\",\"tpl\":\"5aa7e276e5b5b000171d0647\",\"name\":\"Altyn\"}," +
            "{\"slot\":\"ArmorVest\",\"tpl\":\"545cdb794bdc2d3a198b456a\",\"name\":\"6B13\"}]",
            body);
    }

    [Fact]
    public void Player_payload_escapes_weapon_and_equipment_strings()
    {
        var state = TestStates.PlayerState(
            weapon: new WeaponSnapshot("tpl\"x", "a\\b", 0, 0),
            equipment: new[] { new EquipmentEntry("Back\"pack", "tpl", "n\n") });

        var body = BridgePayloads.BuildPlayer(state, nowMs: 1000);

        Assert.Contains("\"tpl\":\"tpl\\\"x\"", body);
        Assert.Contains("\"name\":\"a\\\\b\"", body);
        Assert.Contains("\"slot\":\"Back\\\"pack\"", body);
        Assert.Contains("\"name\":\"n\\n\"", body);
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
            "\"weapon\":null,\"equipment\":[]," +
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
                new HealthSnapshot(true, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f),
                null,
                Array.Empty<EquipmentEntry>()),
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

    // ---------- /raid/events ----------

    private static readonly long EventTicks =
        new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc).Ticks;

    [Fact]
    public void Events_payload_is_empty_for_fresh_buffer()
    {
        var buffer = new RaidEventBuffer();

        var body = BridgePayloads.BuildEvents(buffer, since: 0, limit: 100, inRaid: false);

        Assert.Equal("{\"inRaid\":false,\"seq\":0,\"dropped\":0,\"events\":[]}", body);
    }

    [Fact]
    public void Events_payload_matches_contract_for_damage_death_and_extraction()
    {
        var buffer = new RaidEventBuffer();
        buffer.AppendDamage(
            EventTicks,
            "profile-1@start",
            new DamagePayload("bot-1", false, "Head", 12.5f, "Bullet"));
        buffer.AppendDeath(
            EventTicks,
            "profile-1@start",
            new DeathPayload(
                "bot-1",
                false,
                "Bullet",
                hasKiller: true,
                new KillerInfo("profile-1", "SamMeow", "Usec", "pmcUSEC", true)));
        buffer.AppendExtraction(
            EventTicks,
            "profile-1@start",
            new ExtractionPayload("Gate 3", "Survived"));

        var body = BridgePayloads.BuildEvents(buffer, since: 0, limit: 100, inRaid: true);

        Assert.Equal(
            "{\"inRaid\":true,\"seq\":3,\"dropped\":0,\"events\":[" +
            "{\"seq\":1,\"ts\":\"2026-09-14T00:00:00.0000000Z\",\"type\":\"damage\",\"raidId\":\"profile-1@start\"," +
            "\"payload\":{\"victimProfileId\":\"bot-1\",\"victimIsLocal\":false,\"part\":\"Head\",\"amount\":12.5,\"sourceType\":\"Bullet\"}}," +
            "{\"seq\":2,\"ts\":\"2026-09-14T00:00:00.0000000Z\",\"type\":\"death\",\"raidId\":\"profile-1@start\"," +
            "\"payload\":{\"victimProfileId\":\"bot-1\",\"victimIsLocal\":false,\"damageType\":\"Bullet\"," +
            "\"killer\":{\"profileId\":\"profile-1\",\"name\":\"SamMeow\",\"side\":\"Usec\",\"role\":\"pmcUSEC\",\"isLocal\":true}}}," +
            "{\"seq\":3,\"ts\":\"2026-09-14T00:00:00.0000000Z\",\"type\":\"extraction\",\"raidId\":\"profile-1@start\"," +
            "\"payload\":{\"exitName\":\"Gate 3\",\"status\":\"Survived\"}}" +
            "]}",
            body);
    }

    [Theory]
    [InlineData("Damage", "damage")]
    [InlineData("Death", "death")]
    [InlineData("Extraction", "extraction")]
    public void TypeName_is_stable_literal(string kind, string expected)
    {
        Assert.Equal(expected, RaidEvent.TypeName(Enum.Parse<RaidEventKind>(kind)));
    }

    [Fact]
    public void Unknown_kind_is_not_serialized_as_extraction()
    {
        // 未知 kind 不得伪装成 extraction。
        Assert.Equal("unknown", RaidEvent.TypeName((RaidEventKind)999));
    }

    [Fact]
    public void Serialized_type_matches_TypeName_for_each_kind()
    {
        var buffer = new RaidEventBuffer();
        buffer.AppendDamage(EventTicks, "raid-1", new DamagePayload("bot-1", false, "Head", 1f, "Bullet"));
        buffer.AppendDeath(EventTicks, "raid-1", new DeathPayload("bot-2", false, "Bullet", false, default));
        buffer.AppendExtraction(EventTicks, "raid-1", new ExtractionPayload("Gate 3", "Survived"));

        var body = BridgePayloads.BuildEvents(buffer, since: 0, limit: 100, inRaid: true);

        Assert.Contains($"\"type\":\"{RaidEvent.TypeName(RaidEventKind.Damage)}\"", body);
        Assert.Contains($"\"type\":\"{RaidEvent.TypeName(RaidEventKind.Death)}\"", body);
        Assert.Contains($"\"type\":\"{RaidEvent.TypeName(RaidEventKind.Extraction)}\"", body);
    }

    [Fact]
    public void Events_death_without_attribution_has_null_killer()
    {
        var buffer = new RaidEventBuffer();
        buffer.AppendDeath(
            EventTicks,
            "raid-1",
            new DeathPayload("bot-2", false, "Fall", hasKiller: false, default));

        var body = BridgePayloads.BuildEvents(buffer, since: 0, limit: 100, inRaid: true);

        Assert.Contains("\"damageType\":\"Fall\",\"killer\":null", body);
    }

    [Fact]
    public void Events_payload_returns_only_newer_than_since_and_reports_latest_seq()
    {
        var buffer = new RaidEventBuffer();
        buffer.AppendDamage(EventTicks, "raid-1", new DamagePayload("bot-1", false, "Chest", 1f, "Bullet"));
        buffer.AppendDamage(EventTicks, "raid-1", new DamagePayload("bot-2", false, "Chest", 2f, "Bullet"));
        buffer.AppendDamage(EventTicks, "raid-1", new DamagePayload("bot-3", false, "Chest", 3f, "Bullet"));

        var body = BridgePayloads.BuildEvents(buffer, since: 1, limit: 100, inRaid: true);

        Assert.StartsWith("{\"inRaid\":true,\"seq\":3,\"dropped\":0,\"events\":[", body);
        Assert.DoesNotContain("\"seq\":1,", body);
        Assert.Contains("\"seq\":2,", body);
        Assert.Contains("\"seq\":3,", body);
    }

    [Fact]
    public void Format_timestamp_is_utc_iso_8601()
    {
        Assert.Equal("2026-09-14T00:00:00.0000000Z", BridgePayloads.FormatTimestamp(EventTicks));
        Assert.Equal(string.Empty, BridgePayloads.FormatTimestamp(0));
    }

    // ---------- /logs/recent ----------

    [Fact]
    public void Logs_payload_is_empty_for_fresh_buffer()
    {
        var buffer = new LogRingBuffer();

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 100, minLevel: null);

        Assert.Equal("{\"seq\":0,\"dropped\":0,\"entries\":[]}", body);
    }

    [Fact]
    public void Logs_payload_matches_contract_with_stable_field_order()
    {
        var buffer = new LogRingBuffer();
        buffer.Append(EventTicks, "warning", "Unity", "line1\nline2");
        buffer.Append(EventTicks, "error", "Assembly-CSharp", "boom");

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 100, minLevel: null);

        Assert.Equal(
            "{\"seq\":2,\"dropped\":0,\"entries\":[" +
            "{\"seq\":1,\"ts\":\"2026-09-14T00:00:00.0000000Z\",\"level\":\"warning\",\"source\":\"Unity\",\"text\":\"line1\\nline2\"}," +
            "{\"seq\":2,\"ts\":\"2026-09-14T00:00:00.0000000Z\",\"level\":\"error\",\"source\":\"Assembly-CSharp\",\"text\":\"boom\"}" +
            "]}",
            body);
    }

    [Fact]
    public void Logs_payload_escapes_source_and_text()
    {
        var buffer = new LogRingBuffer();
        buffer.Append(EventTicks, "warning", "a\"b", "c\\d");

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 100, minLevel: null);

        Assert.Contains("\"source\":\"a\\\"b\"", body);
        Assert.Contains("\"text\":\"c\\\\d\"", body);
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData("", 2)]
    [InlineData("debug", 2)]
    [InlineData("info", 2)]
    [InlineData("warning", 2)]
    [InlineData("error", 1)]
    [InlineData("fatal", 0)]
    public void Logs_payload_level_is_a_query_side_minimum(string minLevel, int expectedCount)
    {
        var buffer = new LogRingBuffer();
        buffer.Append(EventTicks, "warning", "src", "w");
        buffer.Append(EventTicks, "error", "src", "e");

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 100, minLevel: minLevel);

        var entries = body.Substring(body.IndexOf("\"entries\":[", StringComparison.Ordinal));
        var count = entries.Split(new[] { "\"seq\":" }, StringSplitOptions.None).Length - 1;
        Assert.Equal(expectedCount, count);
    }

    [Fact]
    public void Logs_payload_level_filter_keeps_seq_and_dropped_untouched()
    {
        var buffer = new LogRingBuffer();
        buffer.Append(EventTicks, "warning", "src", "w");
        buffer.Append(EventTicks, "error", "src", "e");

        var body = BridgePayloads.BuildLogs(buffer, since: 1, limit: 100, minLevel: "error");

        Assert.StartsWith("{\"seq\":2,\"dropped\":0,\"entries\":[", body);
        Assert.DoesNotContain("\"seq\":1,", body);
        Assert.Contains("\"seq\":2,", body);
    }

    [Fact]
    public void Logs_payload_reports_dropped_when_since_is_stale()
    {
        var buffer = new LogRingBuffer(capacity: 2);
        buffer.Append(EventTicks, "warning", "src", "a");
        buffer.Append(EventTicks, "warning", "src", "b");
        buffer.Append(EventTicks, "warning", "src", "c");

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 100, minLevel: null);

        Assert.StartsWith("{\"seq\":3,\"dropped\":1,\"entries\":[", body);
    }

    [Fact]
    public void Logs_payload_filters_before_applying_limit()
    {
        // 回归（starvation）：先截窗后过滤会在窗口内无匹配时返回空、游标无法推进。
        var buffer = new LogRingBuffer(capacity: 10);
        for (var i = 0; i < 5; i++)
        {
            buffer.Append(EventTicks, "warning", "src", "w" + i);
        }

        for (var i = 0; i < 3; i++)
        {
            buffer.Append(EventTicks, "error", "src", "e" + i);
        }

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 2, minLevel: "error");

        // 3 条 error 先通过级别过滤，再取前 2 条（而不是「前 2 条全是 warning → 空」）。
        Assert.StartsWith("{\"seq\":8,\"dropped\":0,\"entries\":[", body);
        Assert.Contains("\"seq\":6,", body);
        Assert.Contains("\"seq\":7,", body);
        Assert.DoesNotContain("\"seq\":5,", body);
    }

    [Fact]
    public void Logs_payload_empty_result_means_no_match_in_the_window()
    {
        var buffer = new LogRingBuffer(capacity: 10);
        for (var i = 0; i < 5; i++)
        {
            buffer.Append(EventTicks, "warning", "src", "w" + i);
        }

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 2, minLevel: "fatal");

        Assert.Equal("{\"seq\":5,\"dropped\":0,\"entries\":[]}", body);
    }

    [Fact]
    public void Logs_payload_limit_truncates_from_the_oldest_side_like_raid_events()
    {
        // 与 /raid/events 一致：limit 约束的是「since 之后最早的 limit 条」，
        // 调用方据 seq 推进游标继续拉取，而不是取最新窗口。
        var buffer = new LogRingBuffer();
        buffer.Append(EventTicks, "warning", "src", "a");
        buffer.Append(EventTicks, "warning", "src", "b");
        buffer.Append(EventTicks, "warning", "src", "c");

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 2, minLevel: null);

        Assert.Equal(
            "{\"seq\":3,\"dropped\":0,\"entries\":[" +
            "{\"seq\":1,\"ts\":\"2026-09-14T00:00:00.0000000Z\",\"level\":\"warning\",\"source\":\"src\",\"text\":\"a\"}," +
            "{\"seq\":2,\"ts\":\"2026-09-14T00:00:00.0000000Z\",\"level\":\"warning\",\"source\":\"src\",\"text\":\"b\"}" +
            "]}",
            body);
    }

    [Fact]
    public void Logs_payload_is_independent_of_raid_state()
    {
        // 契约：/logs/recent 负载不含 inRaid 字段（日志与 raid 无关）。
        var buffer = new LogRingBuffer();
        buffer.Append(EventTicks, "fatal", "src", "crash");

        var body = BridgePayloads.BuildLogs(buffer, since: 0, limit: 100, minLevel: null);

        Assert.DoesNotContain("inRaid", body);
        Assert.Contains("\"level\":\"fatal\"", body);
    }

    // ---------- /logs/summary ----------

    [Fact]
    public void Log_summary_payload_is_empty_for_fresh_store()
    {
        var store = new LogSummaryStore();

        var body = BridgePayloads.BuildLogSummary(store, sinceTicks: 0);

        Assert.Equal("{\"groups\":[],\"overflowDropped\":0}", body);
    }

    [Fact]
    public void Log_summary_payload_matches_contract_with_stable_field_order()
    {
        var store = new LogSummaryStore();
        store.Observe(EventTicks, "error", "Assembly-CSharp", "Failed to load asset 12");
        store.Observe(EventTicks + 10, "error", "Assembly-CSharp", "Failed to load asset 345");

        var body = BridgePayloads.BuildLogSummary(store, sinceTicks: 0);

        Assert.Equal(
            "{\"groups\":[{\"key\":\"Failed to load asset <n>\",\"level\":\"error\"," +
            "\"source\":\"Assembly-CSharp\",\"count\":2," +
            "\"firstTs\":\"2026-09-14T00:00:00.0000000Z\"," +
            "\"lastTs\":\"2026-09-14T00:00:00.0000010Z\"," +
            "\"sampleText\":\"Failed to load asset 12\"}]," +
            "\"overflowDropped\":0}",
            body);
    }

    [Fact]
    public void Log_summary_payload_escapes_key_and_sample_text()
    {
        var store = new LogSummaryStore();
        store.Observe(EventTicks, "warning", "a\"b", "c\\d");

        var body = BridgePayloads.BuildLogSummary(store, sinceTicks: 0);

        Assert.Contains("\"source\":\"a\\\"b\"", body);
        Assert.Contains("\"sampleText\":\"c\\\\d\"", body);
    }

    [Fact]
    public void Log_summary_payload_reports_overflow_dropped()
    {
        var store = new LogSummaryStore(maxGroups: 1);
        store.Observe(EventTicks, "warning", "src", "alpha");
        store.Observe(EventTicks + 1, "warning", "src", "bravo");

        var body = BridgePayloads.BuildLogSummary(store, sinceTicks: 0);

        Assert.StartsWith("{\"groups\":[", body);
        Assert.EndsWith("],\"overflowDropped\":1}", body);
    }

    [Fact]
    public void Log_summary_payload_applies_since_filter()
    {
        var store = new LogSummaryStore();
        store.Observe(EventTicks, "warning", "src", "alpha");
        store.Observe(EventTicks + 100, "warning", "src", "bravo");

        var body = BridgePayloads.BuildLogSummary(store, sinceTicks: EventTicks + 50);

        Assert.Contains("\"key\":\"bravo\"", body);
        Assert.DoesNotContain("\"key\":\"alpha\"", body);
    }

    [Fact]
    public void Log_summary_payload_is_independent_of_raid_state()
    {
        var store = new LogSummaryStore();
        store.Observe(EventTicks, "fatal", "src", "crash");

        var body = BridgePayloads.BuildLogSummary(store, sinceTicks: 0);

        Assert.DoesNotContain("inRaid", body);
    }
}

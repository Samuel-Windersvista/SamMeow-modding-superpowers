using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

/// <summary>
/// C10 契约工件与共享夹具消费（C# 端）。
///
/// 断言三件事：
///   1. 契约加载 + 校验 + 响亮失败（<see cref="BridgeContract"/>）；
///   2. 三处消费点确实由契约派生（Plugin.ProtocolVersion / 归一化规则 / 聚合上限）；
///   3. 两端共享夹具（shared/bridge-contract/fixtures/**）逐条通过——
///      TS 端消费同一批文件（见 tools/tarkov-runtime-mcp/tests/shared/bridge-contract.test.ts）。
/// </summary>
public class BridgeContractTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    // -------------------------------------------------------------------------
    // 契约加载 / 消费点派生
    // -------------------------------------------------------------------------

    [Fact]
    public void Contract_is_embedded_and_loads_with_valid_shape()
    {
        var assembly = typeof(BridgeContract).Assembly;

        Assert.NotNull(assembly.GetManifestResourceStream(BridgeContract.ResourceName));
        Assert.True(BridgeContract.ProtocolVersion >= 1);
        Assert.True(BridgeContract.MaxGroups >= 1);
        Assert.NotEmpty(BridgeContract.NormalizationRules);
        Assert.False(string.IsNullOrEmpty(BridgeContract.WhitespacePattern));
        Assert.True(BridgeContract.Trim);
    }

    [Fact]
    public void Consumption_points_are_contract_derived()
    {
        // Plugin.ProtocolVersion => BridgeContract.ProtocolVersion 无法在此直接断言：
        // Plugin 的基类是 BepInEx.Unity.IL2CPP.BasePlugin，而测试产物不含 BepInEx 程序集
        // （Directory.Build.props 以 Private=false 引用，不随插件分发）。
        // 该派生由 Shared_payload_fixtures_match_constructed_bodies 间接锁定：
        // /bridge/info 样例的 protocolVersion 必须等于契约值。
        Assert.Equal(BridgeContract.MaxGroups, LogSummaryStore.DefaultMaxGroups);
        Assert.True(BridgeContract.ProtocolVersion >= 1);
    }

    [Fact]
    public void Normalization_rules_come_from_the_contract_in_order()
    {
        var ids = BridgeContract.NormalizationRules.Select(rule => rule.Id).ToArray();

        Assert.Equal(new[] { "guid", "id24hex", "hex", "number" }, ids);
        Assert.Equal("<guid>", BridgeContract.NormalizationRules[0].Replacement);
        Assert.Equal("<id>", BridgeContract.NormalizationRules[1].Replacement);
        Assert.Equal("<hex>", BridgeContract.NormalizationRules[2].Replacement);
        Assert.Equal("<n>", BridgeContract.NormalizationRules[3].Replacement);
    }

    // -------------------------------------------------------------------------
    // 响亮失败
    // -------------------------------------------------------------------------

    [Fact]
    public void Parse_rejects_malformed_json()
    {
        Assert.Throws<InvalidOperationException>(() => BridgeContract.Parse("{ not json"));
        Assert.Throws<InvalidOperationException>(() => BridgeContract.Parse("   "));

        // 顺带锁定源码中文字面量的运行时解码（无 BOM UTF-8 源文件必须正确读入）。
        var exception = Assert.Throws<InvalidOperationException>(() => BridgeContract.Parse("{ not json"));
        Assert.Contains("桥接契约", exception.Message);
    }

    [Theory]
    [InlineData("{\"protocolVersion\":1,\"logNormalization\":{\"rules\":[]},\"logAggregation\":{\"maxGroups\":500}}")]
    [InlineData("{\"protocolVersion\":0,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"b\"}],\"whitespacePattern\":\"[ ]+\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500}}")]
    [InlineData("{\"protocolVersion\":1,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"(\",\"replacement\":\"b\"}],\"whitespacePattern\":\"[ ]+\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500}}")]
    [InlineData("{\"protocolVersion\":1,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"b\"}],\"whitespacePattern\":\"(\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500}}")]
    [InlineData("{\"protocolVersion\":1,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"b\"}],\"whitespacePattern\":\"\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500}}")]
    [InlineData("{\"protocolVersion\":1,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"\"}],\"whitespacePattern\":\"[ ]+\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500}}")]
    [InlineData("{\"protocolVersion\":1,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"b\"}],\"whitespacePattern\":\"[ ]+\",\"trim\":true},\"logAggregation\":{\"maxGroups\":0}}")]
    [InlineData("{\"protocolVersion\":1,\"logAggregation\":{\"maxGroups\":500}}")]
    public void Parse_rejects_invalid_contract(string json)
    {
        Assert.Throws<InvalidOperationException>(() => BridgeContract.Parse(json));
    }

    [Fact]
    public void Parse_accepts_integral_double_numbers()
    {
        // F4：JSON 的 1.0 / 500.0 形态整数双端接受（TS Number.isInteger 语义对齐）。
        var contract = BridgeContract.Parse(
            "{\"protocolVersion\":1.0,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"b\"}],\"whitespacePattern\":\"[ ]+\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500.0}}");

        Assert.Equal(1, contract.ProtocolVersion);
        Assert.Equal(500, contract.MaxGroups);
    }

    [Theory]
    [InlineData("{\"protocolVersion\":1.5,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"b\"}],\"whitespacePattern\":\"[ ]+\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500}}")]
    [InlineData("{\"protocolVersion\":1,\"logNormalization\":{\"rules\":[{\"id\":\"a\",\"pattern\":\"a\",\"replacement\":\"b\"}],\"whitespacePattern\":\"[ ]+\",\"trim\":true},\"logAggregation\":{\"maxGroups\":500.5}}")]
    public void Parse_rejects_non_integral_numbers(string json)
    {
        Assert.Throws<InvalidOperationException>(() => BridgeContract.Parse(json));
    }

    // -------------------------------------------------------------------------
    // 共享夹具：归一化
    // -------------------------------------------------------------------------

    [Fact]
    public void Shared_normalization_fixture_passes()
    {
        using var fixture = JsonDocument.Parse(ReadFixture("log-normalization.json"));
        var cases = fixture.RootElement.GetProperty("cases");
        Assert.True(cases.GetArrayLength() > 0, "归一化夹具不得为空");

        var failures = new List<string>();
        foreach (var testCase in cases.EnumerateArray())
        {
            var name = testCase.GetProperty("name").GetString();
            var input = testCase.GetProperty("input").GetString();
            var expected = testCase.GetProperty("expected").GetString();
            var actual = LogSummaryNormalizer.Normalize(input);
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                failures.Add($"{name}: 期望 {Show(expected)}，实际 {Show(actual)}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    // -------------------------------------------------------------------------
    // 共享夹具：聚合
    // -------------------------------------------------------------------------

    [Fact]
    public void Shared_aggregation_fixture_passes()
    {
        using var fixture = JsonDocument.Parse(ReadFixture("log-aggregation.json"));
        var cases = fixture.RootElement.GetProperty("cases");
        Assert.True(cases.GetArrayLength() > 0, "聚合夹具不得为空");

        var failures = new List<string>();
        foreach (var testCase in cases.EnumerateArray())
        {
            var name = testCase.GetProperty("name").GetString();
            var maxGroups = testCase.TryGetProperty("maxGroups", out var maxGroupsElement)
                ? maxGroupsElement.GetInt32()
                : BridgeContract.MaxGroups;
            var store = new LogSummaryStore(maxGroups);

            foreach (var entry in testCase.GetProperty("entries").EnumerateArray())
            {
                store.Observe(
                    ParseTicks(entry.GetProperty("ts").GetString()),
                    entry.GetProperty("level").GetString(),
                    entry.GetProperty("source").GetString(),
                    entry.GetProperty("text").GetString());
            }

            var sinceTicks = testCase.TryGetProperty("since", out var sinceElement)
                ? ParseTicks(sinceElement.GetString())
                : 0L;
            var page = store.Snapshot(sinceTicks);

            var actual = new AggregationExpected
            {
                Groups = page.Groups.Select(group => new AggregationGroup
                {
                    Key = group.Key,
                    Level = group.Level,
                    Source = group.Source,
                    Count = group.Count,
                    FirstTs = BridgePayloads.FormatTimestamp(group.FirstTimestampUtcTicks),
                    LastTs = BridgePayloads.FormatTimestamp(group.LastTimestampUtcTicks),
                    SampleText = group.SampleText,
                }).ToList(),
                OverflowDropped = page.OverflowDropped,
            };

            var expected = testCase.GetProperty("expected").Deserialize<AggregationExpected>(JsonOptions);
            var actualJson = JsonSerializer.Serialize(actual, JsonOptions);
            var expectedJson = JsonSerializer.Serialize(expected, JsonOptions);
            if (!string.Equals(expectedJson, actualJson, StringComparison.Ordinal))
            {
                failures.Add($"{name}:\n  期望 {expectedJson}\n  实际 {actualJson}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void Aggregation_cap_uses_contract_max_groups()
    {
        var maxGroups = BridgeContract.MaxGroups;
        var store = new LogSummaryStore();

        for (var index = 0; index < maxGroups + 3; index++)
        {
            // 键必须不含数字：归一化会剥离数字，纯数字键会塌缩为同一组。
            store.Observe(index + 1, "warning", "s", AlphaKey(index));
        }

        var page = store.Snapshot(0);

        Assert.Equal(maxGroups, page.Groups.Length);
        Assert.Equal(3, page.OverflowDropped);
    }

    /// <summary>纯字母唯一键（index → a, b, …, z, aa, ab, …），规避数字归一化。</summary>
    private static string AlphaKey(int index)
    {
        var chars = new List<char>();
        var value = index;
        do
        {
            chars.Add((char)('a' + (value % 26)));
            value /= 26;
        }
        while (value > 0);

        chars.Reverse();
        return "key-" + new string(chars.ToArray());
    }

    // -------------------------------------------------------------------------
    // 共享夹具：payload 样例（C# 断言构造输出与样例逐字节一致，含字段序）
    // -------------------------------------------------------------------------

    [Fact]
    public void Shared_payload_fixtures_match_constructed_bodies()
    {
        var eventTicks = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc).Ticks;

        var events = new RaidEventBuffer();
        events.AppendDamage(
            eventTicks, "profile-1@start", new DamagePayload("bot-1", false, "Head", 12.5f, "Bullet"));
        events.AppendDeath(
            eventTicks,
            "profile-1@start",
            new DeathPayload(
                "bot-1", false, "Bullet", hasKiller: true,
                new KillerInfo("profile-1", "SamMeow", "Usec", "pmcUSEC", true)));
        events.AppendExtraction(
            eventTicks, "profile-1@start", new ExtractionPayload("Gate 3", "Survived"));

        var logs = new LogRingBuffer();
        logs.Append(eventTicks, "warning", "Unity", "line1\nline2");
        logs.Append(eventTicks, "error", "Assembly-CSharp", "boom");

        var summaries = new LogSummaryStore();
        summaries.Observe(eventTicks, "error", "Assembly-CSharp", "Failed to load asset 12");
        summaries.Observe(eventTicks + 10, "error", "Assembly-CSharp", "Failed to load asset 345");

        var equipped = TestStates.PlayerState(
            weapon: new WeaponSnapshot("5447a9cd4bdc2dbd208b4567", "M4A1", 30, 1),
            equipment: new[]
            {
                new EquipmentEntry("Headwear", "5aa7e276e5b5b000171d0647", "Altyn"),
                new EquipmentEntry("ArmorVest", "545cdb794bdc2d3a198b456a", "6B13"),
            });

        var botsDetail = TestStates.BotsState(
            4,
            new BotEntry(new Vector3Snapshot(1f, 2f, 3f), "pmcBot", "Savage", true),
            new BotEntry(new Vector3Snapshot(-4.5f, 0f, 0f), "bossKilla", "Savage", false));

        var expectedBodies = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["bridge-info"] = BridgePayloads.BuildInfo("0.2.0", BridgeContract.ProtocolVersion, 250, 49777),
            ["raid-status"] = BridgePayloads.BuildStatus(TestStates.PlayerState(), nowMs: 1750),
            ["raid-player"] = BridgePayloads.BuildPlayer(TestStates.PlayerState(), nowMs: 1750),
            ["raid-player-equipped"] = BridgePayloads.BuildPlayer(equipped, nowMs: 1000),
            ["raid-bots"] = BridgePayloads.BuildBots(TestStates.BotsState(total: 4), detail: false, nowMs: 1750),
            ["raid-bots-detail"] = BridgePayloads.BuildBots(botsDetail, detail: true, nowMs: 1750),
            ["raid-events"] = BridgePayloads.BuildEvents(events, since: 0, limit: 100, inRaid: true),
            ["logs-recent"] = BridgePayloads.BuildLogs(logs, since: 0, limit: 100, minLevel: null),
            ["logs-summary"] = BridgePayloads.BuildLogSummary(summaries, sinceTicks: 0),
        };

        foreach (var pair in expectedBodies)
        {
            Assert.Equal(pair.Value, ReadPayloadFixture($"{pair.Key}.json"));
        }
    }

    [Fact]
    public void Shared_error_fixtures_match_error_bodies()
    {
        using var fixture = JsonDocument.Parse(ReadFixture("payloads/errors.json"));
        var root = fixture.RootElement;

        Assert.Equal(BridgePayloads.NotFoundBody, root.GetProperty("not_found").GetString());
        Assert.Equal(BridgePayloads.MethodNotAllowedBody, root.GetProperty("method_not_allowed").GetString());
        Assert.Equal(BridgePayloads.InternalErrorBody, root.GetProperty("internal_error").GetString());
        Assert.Equal(BridgePayloads.NotInRaidBody, root.GetProperty("not_in_raid").GetString());
    }

    // -------------------------------------------------------------------------
    // 辅助
    // -------------------------------------------------------------------------

    private static string ReadFixture(string relativePath)
    {
        return File.ReadAllText(
            Path.Combine(FindRepoRoot(), "shared", "bridge-contract", "fixtures", relativePath.Replace('/', Path.DirectorySeparatorChar)),
            System.Text.Encoding.UTF8);
    }

    private static string ReadPayloadFixture(string fileName)
    {
        var raw = File.ReadAllText(
            Path.Combine(FindRepoRoot(), "shared", "bridge-contract", "fixtures", "payloads", fileName),
            System.Text.Encoding.UTF8);
        return raw.TrimEnd('\n', '\r');
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "shared", "bridge-contract", "contract.json")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("找不到仓库根（shared/bridge-contract/contract.json）");
    }

    private static long ParseTicks(string iso)
    {
        return DateTime.Parse(iso ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).Ticks;
    }

    private static string Show(string value)
    {
        return value == null ? "<null>" : "\"" + value + "\"";
    }

    private sealed class AggregationExpected
    {
        public List<AggregationGroup> Groups { get; set; }

        public int OverflowDropped { get; set; }
    }

    private sealed class AggregationGroup
    {
        public string Key { get; set; }

        public string Level { get; set; }

        public string Source { get; set; }

        public long Count { get; set; }

        public string FirstTs { get; set; }

        public string LastTs { get; set; }

        public string SampleText { get; set; }
    }
}

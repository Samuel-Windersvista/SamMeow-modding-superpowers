using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

/// <summary>
/// C10 pre/post 捕获装置（离线工具，非行为测试）。
///
/// 正常 <c>dotnet test</c> 运行时空操作（环境变量未设）；设置
/// <c>BRIDGE_GOLDEN_DIR=&lt;输出目录&gt;</c> 后写出该阶段的两端可对照 golden：
///   - <c>cs-normalization.json</c>：语料的归一化输出；
///   - <c>cs-aggregation.json</c>：语料的聚合输出（wire 形状，ts 为 C# 往返 ISO）；
///   - <c>cs-payloads.json</c>：各端点 payload 构造输出 + 错误形状。
///
/// 语料 = <c>.scratch/c10-bridge-contract/tools/corpus/</c>（固定输入；期望值由捕获产出）。
/// 复跑入口：<c>.scratch/c10-bridge-contract/tools/capture-goldens.ps1 -Phase pre|post</c>。
/// </summary>
public class GoldenCaptureTests
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    [Fact]
    public void Captures_goldens_when_BRIDGE_GOLDEN_DIR_is_set()
    {
        var outputDir = Environment.GetEnvironmentVariable("BRIDGE_GOLDEN_DIR");
        if (string.IsNullOrWhiteSpace(outputDir))
        {
            // 常规套件运行：空操作（捕获装置由脚本显式驱动）。
            return;
        }

        Directory.CreateDirectory(outputDir);
        var corpusDir = FindCorpusDir();

        WriteJson(
            Path.Combine(outputDir, "cs-normalization.json"),
            CaptureNormalization(Path.Combine(corpusDir, "normalization-cases.json")));
        WriteJson(
            Path.Combine(outputDir, "cs-aggregation.json"),
            CaptureAggregation(Path.Combine(corpusDir, "aggregation-cases.json")));
        WriteJson(
            Path.Combine(outputDir, "cs-payloads.json"),
            CapturePayloads());
    }

    // -------------------------------------------------------------------------
    // 语料定位
    // -------------------------------------------------------------------------

    private static string FindCorpusDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(
                dir.FullName, ".scratch", "c10-bridge-contract", "tools", "corpus");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException(
            "找不到 C10 语料目录：.scratch/c10-bridge-contract/tools/corpus");
    }

    private static void WriteJson(string path, object document)
    {
        var json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(path, json + "\n", new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    // -------------------------------------------------------------------------
    // 归一化
    // -------------------------------------------------------------------------

    private static NormalizationDocument CaptureNormalization(string corpusPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(corpusPath, Encoding.UTF8));
        var result = new NormalizationDocument();

        foreach (var element in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var input = element.GetProperty("input").GetString() ?? string.Empty;
            result.Cases.Add(new NormalizationCase
            {
                Name = element.GetProperty("name").GetString() ?? string.Empty,
                Input = input,
                Output = LogSummaryNormalizer.Normalize(input),
            });
        }

        return result;
    }

    // -------------------------------------------------------------------------
    // 聚合
    // -------------------------------------------------------------------------

    private static AggregationDocument CaptureAggregation(string corpusPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(corpusPath, Encoding.UTF8));
        var result = new AggregationDocument();

        foreach (var element in document.RootElement.GetProperty("cases").EnumerateArray())
        {
            var maxGroups = element.TryGetProperty("maxGroups", out var maxGroupsElement)
                ? maxGroupsElement.GetInt32()
                : LogSummaryStore.DefaultMaxGroups;
            var store = new LogSummaryStore(maxGroups);

            foreach (var entry in element.GetProperty("entries").EnumerateArray())
            {
                store.Observe(
                    ParseTicks(entry.GetProperty("ts").GetString()),
                    entry.GetProperty("level").GetString(),
                    entry.GetProperty("source").GetString(),
                    entry.GetProperty("text").GetString());
            }

            var sinceTicks = element.TryGetProperty("since", out var sinceElement)
                ? ParseTicks(sinceElement.GetString())
                : 0L;
            var page = store.Snapshot(sinceTicks);

            var captureCase = new AggregationCase
            {
                Name = element.GetProperty("name").GetString() ?? string.Empty,
                OverflowDropped = page.OverflowDropped,
            };

            foreach (var group in page.Groups)
            {
                captureCase.Groups.Add(new AggregationGroup
                {
                    Key = group.Key,
                    Level = group.Level,
                    Source = group.Source,
                    Count = group.Count,
                    FirstTs = BridgePayloads.FormatTimestamp(group.FirstTimestampUtcTicks),
                    LastTs = BridgePayloads.FormatTimestamp(group.LastTimestampUtcTicks),
                    SampleText = group.SampleText,
                });
            }

            result.Cases.Add(captureCase);
        }

        return result;
    }

    private static long ParseTicks(string iso)
    {
        return DateTime.Parse(
            iso ?? string.Empty,
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind).Ticks;
    }

    // -------------------------------------------------------------------------
    // payload 样例
    // -------------------------------------------------------------------------

    private static PayloadDocument CapturePayloads()
    {
        var eventTicks = new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc).Ticks;

        var events = new RaidEventBuffer();
        events.AppendDamage(
            eventTicks,
            "profile-1@start",
            new DamagePayload("bot-1", false, "Head", 12.5f, "Bullet"));
        events.AppendDeath(
            eventTicks,
            "profile-1@start",
            new DeathPayload(
                "bot-1",
                false,
                "Bullet",
                hasKiller: true,
                new KillerInfo("profile-1", "SamMeow", "Usec", "pmcUSEC", true)));
        events.AppendExtraction(
            eventTicks,
            "profile-1@start",
            new ExtractionPayload("Gate 3", "Survived"));

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

        var payloads = new PayloadDocument();
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "bridge-info",
            // protocolVersion 用字面量而非 Plugin.ProtocolVersion：捕获装置须在重构前后
            // 可原样复跑（Plugin 类型依赖 BepInEx 基类，测试产物不含该程序集）。
            Body = BridgePayloads.BuildInfo(Plugin.PluginVersion, 1, 250, 49777),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "raid-status",
            Body = BridgePayloads.BuildStatus(TestStates.PlayerState(), nowMs: 1750),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "raid-player",
            Body = BridgePayloads.BuildPlayer(TestStates.PlayerState(), nowMs: 1750),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "raid-player-equipped",
            Body = BridgePayloads.BuildPlayer(equipped, nowMs: 1000),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "raid-bots",
            Body = BridgePayloads.BuildBots(TestStates.BotsState(total: 4), detail: false, nowMs: 1750),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "raid-bots-detail",
            Body = BridgePayloads.BuildBots(botsDetail, detail: true, nowMs: 1750),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "raid-events",
            Body = BridgePayloads.BuildEvents(events, since: 0, limit: 100, inRaid: true),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "logs-recent",
            Body = BridgePayloads.BuildLogs(logs, since: 0, limit: 100, minLevel: null),
        });
        payloads.Bodies.Add(new PayloadBody
        {
            Name = "logs-summary",
            Body = BridgePayloads.BuildLogSummary(summaries, sinceTicks: 0),
        });

        payloads.Errors.Add(new PayloadBody { Name = "not_found", Body = BridgePayloads.NotFoundBody });
        payloads.Errors.Add(new PayloadBody { Name = "method_not_allowed", Body = BridgePayloads.MethodNotAllowedBody });
        payloads.Errors.Add(new PayloadBody { Name = "internal_error", Body = BridgePayloads.InternalErrorBody });
        payloads.Errors.Add(new PayloadBody { Name = "not_in_raid", Body = BridgePayloads.NotInRaidBody });

        return payloads;
    }

    // -------------------------------------------------------------------------
    // 捕获文档形状（与 TS 捕获脚本同形；camelCase 序列化）
    // -------------------------------------------------------------------------

    private sealed class NormalizationDocument
    {
        public List<NormalizationCase> Cases { get; } = new List<NormalizationCase>();
    }

    private sealed class NormalizationCase
    {
        public string Name { get; set; }

        public string Input { get; set; }

        public string Output { get; set; }
    }

    private sealed class AggregationDocument
    {
        public List<AggregationCase> Cases { get; } = new List<AggregationCase>();
    }

    private sealed class AggregationCase
    {
        public string Name { get; set; }

        public List<AggregationGroup> Groups { get; } = new List<AggregationGroup>();

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

    private sealed class PayloadDocument
    {
        public List<PayloadBody> Bodies { get; } = new List<PayloadBody>();

        public List<PayloadBody> Errors { get; } = new List<PayloadBody>();
    }

    private sealed class PayloadBody
    {
        public string Name { get; set; }

        public string Body { get; set; }
    }
}

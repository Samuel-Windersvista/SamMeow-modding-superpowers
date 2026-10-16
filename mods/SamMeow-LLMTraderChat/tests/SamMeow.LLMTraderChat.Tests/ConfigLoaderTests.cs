using SamMeow.LLMTraderChat.Server.Config;
using SamMeow.LLMTraderChat.Shared;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>
/// 配置装载层单测（工单 03）。
/// 覆盖：默认值、缺字段、非法 JSON、启用/禁用各路径；不触碰网络与游戏安装。
/// </summary>
public class ConfigLoaderTests
{
    [Fact]
    public void Parse_EmptyObject_UsesDefaults_WithoutWarnings()
    {
        var result = ConfigLoader.Parse("{}");

        Assert.Empty(result.Warnings);
        Assert.Equal("http://127.0.0.1:11434/v1", result.Config.Llm.Endpoint);
        Assert.Equal("llama3.1", result.Config.Llm.Model);
        Assert.Equal(0.7, result.Config.Llm.Temperature);
        Assert.Equal(512, result.Config.Llm.MaxTokens);
        Assert.Equal(60, result.Config.Llm.TimeoutSeconds);
        Assert.Equal(2, result.Config.ReplyDelay.MinSeconds);
        Assert.Equal(6, result.Config.ReplyDelay.MaxSeconds);
        Assert.Equal(20, result.Config.HistoryMessageCount);
        Assert.Equal(ModConfig.DefaultFallbackMessage, result.Config.FallbackMessage);
        Assert.Equal("config/prompts/system-zh.txt", result.Config.SystemPromptPath);
        Assert.Empty(result.Config.Traders);
    }

    [Fact]
    public void Parse_InvalidJson_DisablesAllTraders_AndWarnsWithFileAndReason()
    {
        var result = ConfigLoader.Parse("{ this is not json }");

        Assert.Single(result.Warnings);
        Assert.Contains("config.jsonc", result.Warnings[0], StringComparison.Ordinal);
        Assert.Empty(result.Config.Traders);
        // 全局字段回退默认值
        Assert.Equal("llama3.1", result.Config.Llm.Model);
    }

    [Fact]
    public void Parse_RootNotObject_DisablesAllTraders_WithWarning()
    {
        var result = ConfigLoader.Parse("[1, 2, 3]");

        Assert.Single(result.Warnings);
        Assert.Empty(result.Config.Traders);
    }

    [Fact]
    public void Parse_TraderMissingTraderId_DropsOnlyThatTrader_AndWarnsWithFieldPath()
    {
        const string json = """
        {
          "traders": [
            { "enabled": true, "persona": "无 id 的商人" },
            { "traderId": "54cb57776803fa99248b456e", "enabled": true }
          ]
        }
        """;

        var result = ConfigLoader.Parse(json);

        // 仅非法商人被移除，合法商人保留
        Assert.Single(result.Config.Traders);
        Assert.Equal("54cb57776803fa99248b456e", result.Config.Traders[0].TraderId);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("config.jsonc", StringComparison.Ordinal)
                       && warning.Contains("traders[0].traderId", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_DisabledTrader_IsKeptWithEnabledFalse_WithoutWarning()
    {
        const string json = """
        { "traders": [ { "traderId": "abc", "enabled": false } ] }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Empty(result.Warnings);
        Assert.Single(result.Config.Traders);
        Assert.False(result.Config.Traders[0].Enabled);
    }

    [Fact]
    public void Parse_TraderWithAllFields_RoundTrips()
    {
        const string json = """
        {
          "traders": [
            {
              "traderId": "54cb57776803fa99248b456e",
              "enabled": true,
              "persona": "冷静的医生",
              "docsDir": "knowledge/54cb57776803fa99248b456e",
              "openingMessage": "有什么需要？",
              "resetKeywords": ["reset", "重置"]
            }
          ]
        }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Empty(result.Warnings);
        var trader = Assert.Single(result.Config.Traders);
        Assert.Equal("54cb57776803fa99248b456e", trader.TraderId);
        Assert.True(trader.Enabled);
        Assert.Equal("冷静的医生", trader.Persona);
        Assert.Equal("knowledge/54cb57776803fa99248b456e", trader.DocsDir);
        Assert.Equal("有什么需要？", trader.OpeningMessage);
        Assert.Equal(new[] { "reset", "重置" }, trader.ResetKeywords);
    }

    [Fact]
    public void Parse_InvalidGlobalField_FallsBackToDefault_WithFieldPath()
    {
        const string json = """{ "llm": { "temperature": "hot" } }""";

        var result = ConfigLoader.Parse(json);

        Assert.Equal(0.7, result.Config.Llm.Temperature);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("llm.temperature", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_InvalidTraderFieldType_FallsBackToDefault_ButKeepsTrader()
    {
        const string json = """{ "traders": [ { "traderId": "abc", "enabled": "yes" } ] }""";

        var result = ConfigLoader.Parse(json);

        var trader = Assert.Single(result.Config.Traders);
        Assert.True(trader.Enabled); // 回退默认 true
        Assert.DoesNotContain(
            result.Warnings,
            warning => warning.Contains("traderId", StringComparison.Ordinal));
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("traders[0].enabled", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_ResetKeywords_InvalidElementIsIgnored()
    {
        const string json = """{ "traders": [ { "traderId": "abc", "resetKeywords": ["reset", 42] } ] }""";

        var result = ConfigLoader.Parse(json);

        var trader = Assert.Single(result.Config.Traders);
        Assert.Equal(new[] { "reset" }, trader.ResetKeywords);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("traders[0].resetKeywords[1]", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_NonStringTraderId_ProducesSingleWarning_AndDisablesTrader()
    {
        // 回归：traderId 类型非法时只报一条告警（不再「类型非法」+「缺失」双告警）。
        const string json = """{ "traders": [ { "traderId": 12345 } ] }""";

        var result = ConfigLoader.Parse(json);

        Assert.Empty(result.Config.Traders);
        var warnings = result.Warnings
            .Where(warning => warning.Contains("traderId", StringComparison.Ordinal))
            .ToList();
        var warning = Assert.Single(warnings);
        Assert.Contains("traders[0].traderId", warning, StringComparison.Ordinal);
    }

    [Fact]
    public void Parse_InvalidEndpoint_IsTreatedAsUnconfigured_WithWarning()
    {
        // 缺 scheme：Uri 会误判 scheme=localhost，装载层须判非法并视同未配置。
        const string json = """{ "llm": { "endpoint": "localhost:11434/v1" } }""";

        var result = ConfigLoader.Parse(json);

        Assert.Equal("", result.Config.Llm.Endpoint);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("llm.endpoint", StringComparison.Ordinal));
    }

    [Fact]
    public void LoadFromDirectory_MissingFiles_ReturnsDefaults_WithWarning()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), "llmtc-cfg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(emptyDir);
        try
        {
            var result = ConfigLoader.LoadFromDirectory(emptyDir);

            Assert.Single(result.Warnings);
            Assert.Empty(result.Config.Traders);
            Assert.Equal("llama3.1", result.Config.Llm.Model);
        }
        finally
        {
            Directory.Delete(emptyDir, recursive: true);
        }
    }

    [Fact]
    public void ShippedConfigFiles_AreValid_WithoutWarnings_AndContainPilotTraders()
    {
        var repoRoot = TestRepoPaths.FindRepoRoot();
        foreach (var name in new[] { ConfigLoader.ConfigFileName, ConfigLoader.DefaultConfigFileName })
        {
            var path = Path.Combine(repoRoot, "config", name);
            Assert.True(File.Exists(path), $"示例配置缺失：{path}");

            var result = ConfigLoader.LoadFromFile(path);

            // 随包示例的 apiKey 用中文占位符 → 允许且仅允许这一条告警（提示替换真实密钥）；无其他告警。
            Assert.All(result.Warnings, warning => Assert.Contains("llm.apiKey", warning, StringComparison.Ordinal));
            // 全商人接入批次：12 位原版商人 + 7 位 mod 商人，共 19 位，均启用。
            Assert.Equal(19, result.Config.Traders.Count);
            Assert.All(result.Config.Traders, trader => Assert.True(trader.Enabled));
            Assert.All(result.Config.Traders, trader => Assert.False(string.IsNullOrWhiteSpace(trader.Persona)));
            Assert.All(result.Config.Traders, trader => Assert.False(string.IsNullOrWhiteSpace(trader.Name)));
            Assert.Contains(
                result.Config.Traders,
                trader => trader.TraderId == SharedConstants.PainterTraderId);
            Assert.False(string.IsNullOrWhiteSpace(result.Config.ResetConfirmationMessage));
        }
    }

    [Fact]
    public void Parse_ApiKey_PlaceholderWarns_RealAsciiAndEmptyDoNot()
    {
        var placeholder = ConfigLoader.Parse("""{ "llm": { "apiKey": "sk-在此填入你的密钥" } }""");
        Assert.Contains(placeholder.Warnings, warning => warning.Contains("llm.apiKey", StringComparison.Ordinal));

        var realKey = ConfigLoader.Parse("""{ "llm": { "apiKey": "sk-1234567890abcdef" } }""");
        Assert.DoesNotContain(realKey.Warnings, warning => warning.Contains("apiKey", StringComparison.Ordinal));

        var emptyKey = ConfigLoader.Parse("""{ "llm": { "apiKey": "" } }""");
        Assert.DoesNotContain(emptyKey.Warnings, warning => warning.Contains("apiKey", StringComparison.Ordinal));
    }

    [Fact]
    public void ShippedConfigAndDefaultConfig_HaveIdenticalTraderIdSets()
    {
        // NIT-1：两份随包配置的 traders[] traderId 集合必须等值（发布默认与玩家运行时配置不得漂移）。
        var repoRoot = TestRepoPaths.FindRepoRoot();
        var shipped = ConfigLoader.LoadFromFile(Path.Combine(repoRoot, "config", ConfigLoader.ConfigFileName));
        var defaults = ConfigLoader.LoadFromFile(Path.Combine(repoRoot, "config", ConfigLoader.DefaultConfigFileName));

        var shippedIds = shipped.Config.Traders.Select(trader => trader.TraderId).ToHashSet(StringComparer.Ordinal);
        var defaultIds = defaults.Config.Traders.Select(trader => trader.TraderId).ToHashSet(StringComparer.Ordinal);

        Assert.True(
            shippedIds.SetEquals(defaultIds),
            $"config.jsonc 与 defaultConfig.jsonc 的 traders[] traderId 集合不一致："
            + $"config=[{string.Join(",", shippedIds)}] default=[{string.Join(",", defaultIds)}]");
    }

    [Fact]
    public void ShippedKnowledgeDocs_ExistForEachEnabledTrader()
    {
        var repoRoot = TestRepoPaths.FindRepoRoot();
        var result = ConfigLoader.LoadFromFile(Path.Combine(repoRoot, "config", ConfigLoader.ConfigFileName));

        foreach (var trader in result.Config.Traders.Where(candidate => candidate.Enabled))
        {
            var docsDirectory = Path.Combine(repoRoot, trader.DocsDir);
            Assert.True(Directory.Exists(docsDirectory), $"知识目录缺失：{docsDirectory}");
            Assert.NotEmpty(Directory.GetFiles(docsDirectory, "*.md"));
        }
    }

    [Fact]
    public void ModTraderAccess_WiredInShippedConfigAndClientDefaults()
    {
        // NIT-3：工单 08 的「发布默认路径」接入守卫——随包 config.jsonc 里 Painter 一条到位、
        // 客户端默认名单（DefaultEnabledTraderIds，含一处常量登记）与之对齐，且其 docsDir 目录确有知识文档。
        var repoRoot = TestRepoPaths.FindRepoRoot();
        var result = ConfigLoader.LoadFromFile(Path.Combine(repoRoot, "config", ConfigLoader.ConfigFileName));

        var painter = Assert.Single(
            result.Config.Traders, trader => trader.TraderId == SharedConstants.PainterTraderId);
        Assert.True(painter.Enabled);
        Assert.False(string.IsNullOrWhiteSpace(painter.Persona));
        Assert.Equal($"knowledge/traders/{SharedConstants.PainterTraderId}", painter.DocsDir);
        Assert.True(Directory.Exists(Path.Combine(repoRoot, painter.DocsDir)));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(repoRoot, painter.DocsDir), "*.md"));
        Assert.Contains(SharedConstants.PainterTraderId, SharedConstants.DefaultEnabledTraderIds);
    }

    [Fact]
    public void AllShippedTraders_AreWiredIntoClientDefaultList()
    {
        // 全商人接入守卫：随包配置里的每一位启用商人，都必须登记在客户端默认名单
        // （DefaultEnabledTraderIds）中，否则玩家端对该商人的输入闸门不会解锁；名单本身不得有重复项。
        var repoRoot = TestRepoPaths.FindRepoRoot();
        var result = ConfigLoader.LoadFromFile(Path.Combine(repoRoot, "config", ConfigLoader.ConfigFileName));

        Assert.Equal(19, result.Config.Traders.Count);
        Assert.All(result.Config.Traders, trader => Assert.True(trader.Enabled));
        foreach (var trader in result.Config.Traders)
        {
            Assert.Contains(trader.TraderId, SharedConstants.DefaultEnabledTraderIds);
        }

        Assert.Equal(
            SharedConstants.DefaultEnabledTraderIds.Count,
            SharedConstants.DefaultEnabledTraderIds.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Parse_TraderName_And_ResetConfirmation_AreRead()
    {
        const string json = """
        {
          "resetConfirmationMessage": "已重置",
          "traders": [ { "traderId": "abc", "name": "测试商人" } ]
        }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Equal("已重置", result.Config.ResetConfirmationMessage);
        var trader = Assert.Single(result.Config.Traders);
        Assert.Equal("测试商人", trader.Name);
    }

    [Fact]
    public void Parse_HistoryMessageCount_ClampedWithWarning()
    {
        var low = ConfigLoader.Parse("""{ "historyMessageCount": 0 }""");
        Assert.Equal(1, low.Config.HistoryMessageCount);
        Assert.Contains(low.Warnings, warning => warning.Contains("historyMessageCount", StringComparison.Ordinal));

        var high = ConfigLoader.Parse("""{ "historyMessageCount": 9999 }""");
        Assert.Equal(200, high.Config.HistoryMessageCount);
    }

    [Fact]
    public void Parse_KnowledgeTopK_ClampedWithWarning()
    {
        var result = ConfigLoader.Parse("""{ "knowledgeTopK": 999 }""");

        Assert.Equal(20, result.Config.KnowledgeTopK);
        Assert.Contains(result.Warnings, warning => warning.Contains("knowledgeTopK", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_TimeoutSeconds_ClampedWithWarning()
    {
        var result = ConfigLoader.Parse("""{ "llm": { "timeoutSeconds": 0 } }""");

        Assert.Equal(1, result.Config.Llm.TimeoutSeconds);
        Assert.Contains(result.Warnings, warning => warning.Contains("llm.timeoutSeconds", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_Temperature_ClampedWithWarning()
    {
        var result = ConfigLoader.Parse("""{ "llm": { "temperature": 5 } }""");

        Assert.Equal(2, result.Config.Llm.Temperature);
        Assert.Contains(result.Warnings, warning => warning.Contains("llm.temperature", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_ReplyDelay_ReversedBounds_SwappedWithWarning()
    {
        var result = ConfigLoader.Parse("""{ "replyDelay": { "minSeconds": 10, "maxSeconds": 3 } }""");

        Assert.Equal(3, result.Config.ReplyDelay.MinSeconds);
        Assert.Equal(10, result.Config.ReplyDelay.MaxSeconds);
        Assert.Contains(result.Warnings, warning => warning.Contains("replyDelay", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_MaxTokens_ClampedWithWarning()
    {
        var low = ConfigLoader.Parse("""{ "llm": { "maxTokens": 0 } }""");
        Assert.Equal(1, low.Config.Llm.MaxTokens);
        Assert.Contains(low.Warnings, warning => warning.Contains("llm.maxTokens", StringComparison.Ordinal));

        var high = ConfigLoader.Parse("""{ "llm": { "maxTokens": 999999 } }""");
        Assert.Equal(32768, high.Config.Llm.MaxTokens);
        Assert.Contains(high.Warnings, warning => warning.Contains("llm.maxTokens", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_ReplyDelay_SingleSideOutOfRange_ClampedWithWarning()
    {
        var low = ConfigLoader.Parse("""{ "replyDelay": { "minSeconds": -5, "maxSeconds": 3 } }""");
        Assert.Equal(0, low.Config.ReplyDelay.MinSeconds);
        Assert.Equal(3, low.Config.ReplyDelay.MaxSeconds);
        Assert.Contains(low.Warnings, warning => warning.Contains("replyDelay.minSeconds", StringComparison.Ordinal));

        var high = ConfigLoader.Parse("""{ "replyDelay": { "minSeconds": 1, "maxSeconds": 999 } }""");
        Assert.Equal(1, high.Config.ReplyDelay.MinSeconds);
        Assert.Equal(60, high.Config.ReplyDelay.MaxSeconds);
        Assert.Contains(high.Warnings, warning => warning.Contains("replyDelay.maxSeconds", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_DefaultNumerics_ProduceNoClampWarnings()
    {
        var result = ConfigLoader.Parse("{}");

        Assert.DoesNotContain(result.Warnings, warning => warning.Contains("钳制", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_TraderAttitudePrompts_ValidKeysCaseInsensitive_RoundTrips()
    {
        // 工单 14 接缝 1：attitudePrompts 键大小写不敏感，值 trim 后按规范键存储。
        const string json = """
        {
          "traders": [
            {
              "traderId": "abc",
              "attitudePrompts": { "Hostile": " 敌对文案 ", "cold": "冷淡文案", "ALLY": "盟友文案" }
            }
          ]
        }
        """;

        var result = ConfigLoader.Parse(json);

        Assert.Empty(result.Warnings);
        var trader = Assert.Single(result.Config.Traders);
        Assert.Equal(3, trader.AttitudePrompts.Count);
        Assert.Equal("敌对文案", trader.AttitudePrompts["hostile"]);
        Assert.Equal("冷淡文案", trader.AttitudePrompts["COLD"]);
        Assert.Equal("盟友文案", trader.AttitudePrompts["ally"]);
    }

    [Fact]
    public void Parse_TraderAttitudePrompts_UnknownKeyOrInvalidValue_WarnsAndIgnores_WithoutDisablingTrader()
    {
        // 工单 14 接缝 1：未知档位键 / 非字符串值 / 空白字符串仅告警忽略，不禁用商人。
        const string json = """
        {
          "traders": [
            {
              "traderId": "abc",
              "attitudePrompts": { "hostile": "敌对", "unknown": "x", "cold": 42, "familiar": "   " }
            }
          ]
        }
        """;

        var result = ConfigLoader.Parse(json);

        var trader = Assert.Single(result.Config.Traders);
        Assert.True(trader.Enabled);
        Assert.True(trader.AttitudePrompts.ContainsKey("hostile"));
        Assert.Single(trader.AttitudePrompts);
        Assert.Equal("敌对", trader.AttitudePrompts["hostile"]);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("traders[0].attitudePrompts.unknown", StringComparison.Ordinal));
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("traders[0].attitudePrompts.cold", StringComparison.Ordinal));
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("traders[0].attitudePrompts.familiar", StringComparison.Ordinal));
    }

    [Fact]
    public void Parse_TraderAttitudePrompts_NotAnObject_WarnsAndFallsBackToEmpty()
    {
        // 工单 14 接缝 1：attitudePrompts 非对象 → 告警并回退空字典，商人保留。
        const string json = """{ "traders": [ { "traderId": "abc", "attitudePrompts": "nope" } ] }""";

        var result = ConfigLoader.Parse(json);

        var trader = Assert.Single(result.Config.Traders);
        Assert.True(trader.Enabled);
        Assert.Empty(trader.AttitudePrompts);
        Assert.Contains(
            result.Warnings,
            warning => warning.Contains("traders[0].attitudePrompts", StringComparison.Ordinal));
    }
}

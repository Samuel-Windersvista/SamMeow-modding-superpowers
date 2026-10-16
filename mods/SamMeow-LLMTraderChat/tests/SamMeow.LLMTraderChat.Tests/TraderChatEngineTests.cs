using SamMeow.LLMTraderChat.Server.Config;
using SamMeow.LLMTraderChat.Server.Engine;
using SamMeow.LLMTraderChat.Server.Knowledge;
using SamMeow.LLMTraderChat.Server.Llm;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>
/// 引擎核心单测（工单 06）：正常回复、检索片段注入、历史注入条数、LLM 失败兜底、未就绪、
/// reset、开场一次性、超时、模板缺失。全部依赖用测试替身注入，不触网、不依赖 SPT 运行时。
/// </summary>
public class TraderChatEngineTests
{
    private const string TraderId = "trader-1";
    private const string SessionId = "session-1";
    private const string TestTemplate = "PERSONA={{persona}}\nKNOW={{knowledge}}\nHIST={{history}}";

    private static TraderChatEngine BuildEngine(
        ILlmClient? llm = null,
        IKnowledgeRetrieverFactory? retrieverFactory = null,
        IChatHistoryProvider? history = null,
        ITraderChatStateStore? state = null,
        IPromptTemplateProvider? templates = null,
        ModConfig? config = null,
        ITraderRelationshipProvider? relationship = null)
    {
        return new TraderChatEngine(
            config ?? BuildConfig(),
            llm ?? new RecordingLlmClient(LlmCompletionResult.Ok("ok")),
            retrieverFactory ?? new StubRetrieverFactory([]),
            history ?? new InMemoryHistoryProvider(),
            state ?? new InMemoryStateStore(),
            templates ?? new StubPromptTemplateProvider(TestTemplate),
            relationship);
    }

    private static ModConfig BuildConfig(
        bool enabled = true,
        string persona = "冷静的医生",
        string opening = "这里是治疗师。",
        int historyCount = 20,
        int topK = 4)
    {
        return new ModConfig
        {
            HistoryMessageCount = historyCount,
            KnowledgeTopK = topK,
            FallbackMessage = "兜底话术",
            SystemPromptPath = "config/prompts/system-zh.txt",
            ReplyDelay = new ReplyDelayConfig { MinSeconds = 2, MaxSeconds = 6 },
            Traders =
            [
                new TraderConfig
                {
                    TraderId = TraderId,
                    Enabled = enabled,
                    Persona = persona,
                    DocsDir = "knowledge/x",
                    OpeningMessage = opening,
                    ResetKeywords = ["reset", "重置"],
                },
            ],
        };
    }

    [Fact]
    public async Task HandleMessage_NormalReply_ReturnsReplyWithDelayInRange()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("回复内容"));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "你好");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.Equal("回复内容", outcome.Text);
        Assert.InRange(outcome.DelaySeconds, 2, 6);
        Assert.Empty(outcome.Warnings);

        var messages = Assert.IsAssignableFrom<IReadOnlyList<LlmChatMessage>>(llm.LastMessages);
        Assert.Equal(2, messages.Count);
        Assert.Equal("system", messages[0].Role);
        Assert.Contains("冷静的医生", messages[0].Content);
        Assert.Equal("user", messages[1].Role);
        Assert.Equal("你好", messages[1].Content);
    }

    [Fact]
    public async Task HandleMessage_InjectsKnowledgeSnippetsWithSourceLabels()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var factory = new StubRetrieverFactory([new KnowledgeSnippet("faq.md", "保险", "理赔流程说明", 9)]);
        var engine = BuildEngine(llm: llm, retrieverFactory: factory);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "保险怎么理赔");

        var system = llm.LastMessages![0].Content;
        Assert.Contains("faq.md", system, StringComparison.Ordinal);
        Assert.Contains("保险", system, StringComparison.Ordinal);
        Assert.Contains("理赔流程说明", system, StringComparison.Ordinal);
        Assert.Equal(1, outcome.KnowledgeHitCount);
        Assert.NotNull(outcome.KnowledgeSources);
        Assert.Contains("faq.md", outcome.KnowledgeSources!);
        Assert.Equal("保险怎么理赔", factory.LastQuery);
        Assert.Equal(4, factory.LastTopK);
    }

    [Fact]
    public async Task HandleMessage_InjectsRecentHistoryIntoPrompt()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var history = new InMemoryHistoryProvider();
        history.Seed(
            SessionId,
            TraderId,
            new ChatHistoryMessage(ChatRole.Player, "你好"),
            new ChatHistoryMessage(ChatRole.Trader, "需要什么？"));
        var engine = BuildEngine(llm: llm, history: history);

        await engine.HandleMessageAsync(SessionId, TraderId, "在吗");

        var system = llm.LastMessages![0].Content;
        Assert.Contains("玩家：你好", system, StringComparison.Ordinal);
        Assert.Contains("商人：需要什么？", system, StringComparison.Ordinal);
        Assert.Equal(20, history.LastRequestedCount);
    }

    [Fact]
    public async Task HandleMessage_HistoryCountFromConfig_IsPassedToProvider()
    {
        var history = new InMemoryHistoryProvider();
        var engine = BuildEngine(history: history, config: BuildConfig(historyCount: 3));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(3, history.LastRequestedCount);
    }

    [Fact]
    public async Task HandleMessage_LlmFailure_ReturnsFallbackWithWarning()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Fail(LlmFailureKind.HttpError, "boom"));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Fallback, outcome.Kind);
        Assert.Equal("兜底话术", outcome.Text);
        Assert.Contains("boom", outcome.Warnings);
        Assert.InRange(outcome.DelaySeconds, 2, 6);
    }

    [Fact]
    public async Task HandleMessage_LlmTimeout_ReturnsFallback()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Fail(LlmFailureKind.Timeout, "timed out"));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Fallback, outcome.Kind);
        Assert.Contains("timed out", outcome.Warnings);
    }

    [Fact]
    public async Task HandleMessage_LlmNotConfigured_ReturnsNotReady()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Fail(LlmFailureKind.NotConfigured, "llm.endpoint 未配置"));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.NotReady, outcome.Kind);
        Assert.Equal(TraderChatEngine.NotReadyMessage, outcome.Text);
    }

    [Fact]
    public async Task HandleMessage_LlmEmptyContent_ReturnsFallback()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("   "));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Fallback, outcome.Kind);
    }

    [Theory]
    [InlineData("reset")]
    [InlineData("RESET")]
    [InlineData(" 重置 ")]
    public async Task HandleMessage_ResetKeyword_ReturnsResetInstruction_WithoutCallingLlm(string playerText)
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, playerText);

        Assert.Equal(ChatOutcomeKind.Reset, outcome.Kind);
        Assert.True(outcome.ResetHistory);
        Assert.False(llm.WasCalled);
    }

    [Fact]
    public async Task HandleMessage_DisabledTrader_IsIgnored()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm, config: BuildConfig(enabled: false));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Ignored, outcome.Kind);
        Assert.False(llm.WasCalled);
    }

    [Fact]
    public async Task HandleMessage_UnknownTrader_IsIgnored()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, "no-such-trader", "hi");

        Assert.Equal(ChatOutcomeKind.Ignored, outcome.Kind);
        Assert.False(llm.WasCalled);
    }

    [Fact]
    public async Task HandleMessage_EmptyText_IsIgnored()
    {
        var engine = BuildEngine();

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "   ");

        Assert.Equal(ChatOutcomeKind.Ignored, outcome.Kind);
    }

    [Fact]
    public async Task GetOpening_FirstCall_ReturnsOpening()
    {
        var engine = BuildEngine();

        var outcome = await engine.GetOpeningMessageAsync(SessionId, TraderId);

        Assert.Equal(ChatOutcomeKind.Opening, outcome.Kind);
        Assert.Equal("这里是治疗师。", outcome.Text);
    }

    [Fact]
    public async Task GetOpening_SecondCall_IsIgnored_OnceOnly()
    {
        var engine = BuildEngine();

        var first = await engine.GetOpeningMessageAsync(SessionId, TraderId);
        var second = await engine.GetOpeningMessageAsync(SessionId, TraderId);

        Assert.Equal(ChatOutcomeKind.Opening, first.Kind);
        Assert.Equal(ChatOutcomeKind.Ignored, second.Kind);
    }

    [Fact]
    public async Task GetOpening_ConcurrentCalls_OnlyOneReturnsOpening()
    {
        var engine = BuildEngine();

        var outcomes = await Task.WhenAll(
            Enumerable.Range(0, 8).Select(_ => engine.GetOpeningMessageAsync(SessionId, TraderId)));

        Assert.Single(outcomes, outcome => outcome.Kind == ChatOutcomeKind.Opening);
        Assert.Equal(7, outcomes.Count(outcome => outcome.Kind == ChatOutcomeKind.Ignored));
    }

    [Fact]
    public async Task GetOpening_NoOpeningMessage_IsIgnored()
    {
        var engine = BuildEngine(config: BuildConfig(opening: ""));

        var outcome = await engine.GetOpeningMessageAsync(SessionId, TraderId);

        Assert.Equal(ChatOutcomeKind.Ignored, outcome.Kind);
    }

    [Fact]
    public async Task HandleMessage_MissingTemplate_UsesBuiltInFallback_WithWarning()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "llmtc-prompt-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);
        try
        {
            var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
            var engine = BuildEngine(llm: llm, templates: new FilePromptTemplateProvider(tempRoot));

            var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

            Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
            Assert.Contains(outcome.Warnings, warning => warning.Contains("system prompt", StringComparison.Ordinal));
            // 内置兜底模板仍被渲染（占位符已被替换，不留 {{persona}}）。
            Assert.Contains("冷静的医生", llm.LastMessages![0].Content, StringComparison.Ordinal);
            Assert.DoesNotContain("{{persona}}", llm.LastMessages[0].Content, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void FilePromptTemplateProvider_ReadsExistingTemplate()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "llmtc-prompt-" + Guid.NewGuid().ToString("N"));
        var relative = "config/prompts/system-zh.txt";
        var fullPath = Path.Combine(tempRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, "CUSTOM {{persona}}");
        try
        {
            var provider = new FilePromptTemplateProvider(tempRoot);

            var result = provider.GetSystemPrompt(relative);

            Assert.False(result.UsedFallback);
            Assert.Null(result.Warning);
            Assert.Equal("CUSTOM {{persona}}", result.Template);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public void ShippedSystemPromptTemplate_MatchesBuiltInDefault()
    {
        var path = Path.Combine(TestRepoPaths.FindRepoRoot(), "config", "prompts", "system-zh.txt");
        Assert.True(File.Exists(path), $"缺少模板文件：{path}");

        var content = File.ReadAllText(path);
        Assert.Equal(SystemPromptDefaults.Template.Trim(), content.Trim());
        Assert.Contains("{{persona}}", content, StringComparison.Ordinal);
        Assert.Contains("{{attitude}}", content, StringComparison.Ordinal);
        Assert.Contains("{{quests}}", content, StringComparison.Ordinal);
        Assert.Contains("{{knowledge}}", content, StringComparison.Ordinal);
        Assert.Contains("{{history}}", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_DuplicateSourceSnippets_AreDeduplicatedInSources()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var factory = new StubRetrieverFactory(
        [
            new KnowledgeSnippet("faq.md", "保险", "甲", 5),
            new KnowledgeSnippet("faq.md", "理赔", "乙", 4),
        ]);
        var engine = BuildEngine(llm: llm, retrieverFactory: factory);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "保险");

        Assert.Equal(2, outcome.KnowledgeHitCount);
        Assert.NotNull(outcome.KnowledgeSources);
        var source = Assert.Single(outcome.KnowledgeSources!);
        Assert.Equal("faq.md", source);
    }

    [Fact]
    public async Task HandleMessage_NoKnowledgeHit_PromptNotesNoSnippets_AndCountZero()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm, retrieverFactory: new StubRetrieverFactory([]));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "随便聊两句");

        Assert.Equal(0, outcome.KnowledgeHitCount);
        Assert.True(outcome.KnowledgeSources is null or { Count: 0 });
        // 无命中时 prompt 明确标注「无相关知识片段」（模型据此自然降级，不硬答）。
        Assert.Contains("（无相关知识片段）", llm.LastMessages![0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_OverBudgetHistory_TruncatesOldestAndWarns()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var history = new InMemoryHistoryProvider();
        var messages = new List<ChatHistoryMessage>
        {
            new(ChatRole.Player, "OLD_MARKER" + new string('a', 600)),
        };
        for (var index = 0; index < 9; index++)
        {
            messages.Add(new ChatHistoryMessage(ChatRole.Trader, "recent-" + index + new string('b', 600)));
        }

        history.Seed(SessionId, TraderId, [.. messages]);
        var engine = BuildEngine(llm: llm, history: history);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.Contains(outcome.Warnings, warning => warning.Contains("历史渲染截断", StringComparison.Ordinal));
        // 总量超预算时优先保留最近；最旧一条被丢弃。
        Assert.DoesNotContain("OLD_MARKER", llm.LastMessages![0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_WithinBudgetHistory_AddsNoTruncationWarning()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var history = new InMemoryHistoryProvider();
        history.Seed(
            SessionId,
            TraderId,
            new ChatHistoryMessage(ChatRole.Player, "你好"),
            new ChatHistoryMessage(ChatRole.Trader, "需要什么？"));
        var engine = BuildEngine(llm: llm, history: history);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.DoesNotContain(outcome.Warnings, warning => warning.Contains("截断", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleMessage_OverBudgetKnowledge_TruncatesAndWarns()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var snippets = Enumerable.Range(0, 5)
            .Select(index => new KnowledgeSnippet($"doc{index}.md", $"T{index}", new string('x', 900), 10 - index))
            .ToList();
        var engine = BuildEngine(llm: llm, retrieverFactory: new StubRetrieverFactory(snippets));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.Contains(outcome.Warnings, warning => warning.Contains("知识渲染截断", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleMessage_WithinBudgetKnowledge_AddsNoTruncationWarning()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(
            llm: llm,
            retrieverFactory: new StubRetrieverFactory([new KnowledgeSnippet("faq.md", "保险", "理赔流程", 5)]));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.DoesNotContain(outcome.Warnings, warning => warning.Contains("截断", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleMessage_PersonaWithPlaceholderLiteral_IsNotReExpanded()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(
            llm: llm,
            retrieverFactory: new StubRetrieverFactory([new KnowledgeSnippet("faq.md", "保险", "SNIPPET_TEXT", 5)]),
            config: BuildConfig(persona: "A{{knowledge}}B"));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        var system = llm.LastMessages![0].Content;
        // 单趟替换：人格里的占位符字面量原样保留，不被二次展开。
        Assert.Contains("A{{knowledge}}B", system, StringComparison.Ordinal);
        Assert.DoesNotContain("A[1] 来源", system, StringComparison.Ordinal);
        // 真占位符照常展开。
        Assert.Contains("SNIPPET_TEXT", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_EmptyFallbackMessage_UsesBuiltInDefault()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Fail(LlmFailureKind.HttpError, "boom"));
        var config = BuildConfig();
        config.FallbackMessage = "";
        var engine = BuildEngine(llm: llm, config: config);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Fallback, outcome.Kind);
        Assert.Equal(ModConfig.DefaultFallbackMessage, outcome.Text);
    }

    [Fact]
    public async Task HandleMessage_CallerCancellation_PropagatesFromRetriever()
    {
        var engine = BuildEngine(retrieverFactory: new CancellingRetrieverFactory());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => engine.HandleMessageAsync(SessionId, TraderId, "hi", cts.Token));
    }

    [Fact]
    public async Task HandleMessage_ResetKeywordWithinSentence_DoesNotTriggerReset()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "请帮我重置一下");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.True(llm.WasCalled);
    }

    [Fact]
    public async Task HandleMessage_ResetKeyword_ReturnsConfiguredConfirmation()
    {
        var config = BuildConfig();
        config.ResetConfirmationMessage = "已重置对话。";
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm, config: config);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "reset");

        Assert.Equal(ChatOutcomeKind.Reset, outcome.Kind);
        Assert.True(outcome.ResetHistory);
        Assert.Equal("已重置对话。", outcome.Text);
        Assert.InRange(outcome.DelaySeconds, 2, 6);
        Assert.False(llm.WasCalled);
    }

    [Fact]
    public async Task HandleMessage_ResetKeyword_EmptyConfirmation_NoTextNoDelay()
    {
        var config = BuildConfig();
        config.ResetConfirmationMessage = "";
        var engine = BuildEngine(config: config);

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "重置");

        Assert.Equal(ChatOutcomeKind.Reset, outcome.Kind);
        Assert.True(outcome.ResetHistory);
        Assert.Equal(string.Empty, outcome.Text);
        Assert.Equal(0, outcome.DelaySeconds);
    }

    [Fact]
    public async Task HandleMessage_NullRelationship_ReportsColdTierAndSummary()
    {
        // 工单 14：关系快照缺失（默认实现返回 null）→ Cold 档，诊断串可读。
        var engine = BuildEngine(relationship: new StubRelationshipProvider(null));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.Equal(AttitudeTier.Cold, outcome.AttitudeTier);
        Assert.NotNull(outcome.AttitudeSummary);
        Assert.Contains("Cold", outcome.AttitudeSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_LegacyTemplateWithoutAttitude_AppendsAttitudeBlockToPersona()
    {
        // 向后兼容：不含 {{attitude}} 的旧模板把态度块拼入 persona 展开结果。
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(
            llm: llm,
            relationship: new StubRelationshipProvider(new TraderRelationshipSnapshot(LoyaltyLevel: 3)));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        var system = llm.LastMessages![0].Content;
        Assert.Contains("冷静的医生", system, StringComparison.Ordinal);
        Assert.Contains("当前对玩家的态度与关系背景：", system, StringComparison.Ordinal);
        Assert.Contains("【信任/器重】", system, StringComparison.Ordinal);
        Assert.Contains("表达禁令", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_AttitudeTemplate_ReplacesPlaceholder()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(
            llm: llm,
            templates: new StubPromptTemplateProvider("P={{persona}}\nA={{attitude}}"),
            relationship: new StubRelationshipProvider(new TraderRelationshipSnapshot(LoyaltyLevel: 2)));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        var system = llm.LastMessages![0].Content;
        Assert.DoesNotContain("{{attitude}}", system, StringComparison.Ordinal);
        Assert.Contains("A=关系阶段：", system, StringComparison.Ordinal);
        Assert.Contains("【熟稔/认可】", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_AttitudePromptsOverride_UsedForResolvedTier_OthersFallBackToBuiltIn()
    {
        var config = BuildConfig();
        config.Traders[0].AttitudePrompts["trusted"] = "自定义信任文案";
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(
            llm: llm,
            config: config,
            relationship: new StubRelationshipProvider(new TraderRelationshipSnapshot(LoyaltyLevel: 3)));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        var system = llm.LastMessages![0].Content;
        Assert.Contains("自定义信任文案", system, StringComparison.Ordinal);
        Assert.DoesNotContain("【信任/器重】", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_RelationshipProviderThrows_WarnsAndFallsBackToCold()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm, relationship: new ThrowingRelationshipProvider("boom"));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.Equal(AttitudeTier.Cold, outcome.AttitudeTier);
        Assert.Contains(
            outcome.Warnings,
            warning => warning.Contains("玩家关系状态读取失败", StringComparison.Ordinal)
                       && warning.Contains("boom", StringComparison.Ordinal));
    }

    [Fact]
    public async Task HandleMessage_RelationshipTier_CarriedIntoFallbackOutcome()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Fail(LlmFailureKind.HttpError, "boom"));
        var engine = BuildEngine(
            llm: llm,
            relationship: new StubRelationshipProvider(new TraderRelationshipSnapshot(LoyaltyLevel: 4)));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Fallback, outcome.Kind);
        Assert.Equal(AttitudeTier.Ally, outcome.AttitudeTier);
        Assert.Contains("Ally", outcome.AttitudeSummary!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_RelationshipCancellation_Propagates()
    {
        var engine = BuildEngine(relationship: new CancellingRelationshipProvider());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => engine.HandleMessageAsync(SessionId, TraderId, "hi", cts.Token));
    }

    [Fact]
    public async Task HandleMessage_AttitudeOverrideWithPlaceholderLiteral_IsNotReExpanded()
    {
        var config = BuildConfig();
        config.Traders[0].AttitudePrompts["trusted"] = "A{{knowledge}}B";
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(
            llm: llm,
            config: config,
            templates: new StubPromptTemplateProvider("{{attitude}}"),
            retrieverFactory: new StubRetrieverFactory([new KnowledgeSnippet("faq.md", "保险", "SNIPPET_TEXT", 5)]),
            relationship: new StubRelationshipProvider(new TraderRelationshipSnapshot(LoyaltyLevel: 3)));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        var system = llm.LastMessages![0].Content;
        Assert.Contains("A{{knowledge}}B", system, StringComparison.Ordinal);
        Assert.DoesNotContain("SNIPPET_TEXT", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_TemplateWithoutPersonaAttitudeOrQuests_WarnsBlocksNotInjected()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(
            llm: llm,
            templates: new StubPromptTemplateProvider("STATIC ONLY"),
            relationship: new StubRelationshipProvider(new TraderRelationshipSnapshot(LoyaltyLevel: 3)));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal(ChatOutcomeKind.Reply, outcome.Kind);
        Assert.Contains(
            outcome.Warnings,
            warning => warning.Contains("未注入", StringComparison.Ordinal));
        Assert.Equal("STATIC ONLY", llm.LastMessages![0].Content);
    }

    [Fact]
    public async Task HandleMessage_ColdTier_DoesNotInjectQuestDetails_ButReportsNoneSummary()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var snapshot = new TraderRelationshipSnapshot(
            LoyaltyLevel: 1,
            ActiveQuestDetails:
            [
                new TraderQuestDetail("水瓶座行动 - 1", ["在海关找到被藏在宿舍楼里的水"], "描述", "海关"),
            ]);
        var engine = BuildEngine(llm: llm, relationship: new StubRelationshipProvider(snapshot));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "水瓶座行动要什么钥匙");

        Assert.Equal("Disclosure=None quests=0", outcome.QuestSummary);
        Assert.DoesNotContain("水瓶座行动", llm.LastMessages![0].Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_FamiliarTier_InjectsHint_WithoutObjectiveVerbatim()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var snapshot = new TraderRelationshipSnapshot(
            LoyaltyLevel: 2,
            ActiveQuestDetails:
            [
                new TraderQuestDetail("水瓶座行动 - 1", ["在海关找到被藏在宿舍楼里的水"], "详细描述文本", "海关"),
            ]);
        var engine = BuildEngine(llm: llm, relationship: new StubRelationshipProvider(snapshot));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "水瓶座行动要什么钥匙");

        Assert.Equal("Disclosure=Hint quests=1", outcome.QuestSummary);
        var system = llm.LastMessages![0].Content;
        Assert.Contains("水瓶座行动 - 1", system, StringComparison.Ordinal);
        Assert.Contains("地区：海关", system, StringComparison.Ordinal);
        Assert.DoesNotContain("宿舍楼", system, StringComparison.Ordinal);
        Assert.DoesNotContain("详细描述文本", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_TrustedTier_InjectsFullQuestDetails()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var snapshot = new TraderRelationshipSnapshot(
            LoyaltyLevel: 3,
            ActiveQuestDetails:
            [
                new TraderQuestDetail("水瓶座行动 - 1", ["在海关找到被藏在宿舍楼里的水"], "详细描述文本", "海关"),
            ]);
        var engine = BuildEngine(llm: llm, relationship: new StubRelationshipProvider(snapshot));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "水瓶座行动要什么钥匙");

        Assert.Equal("Disclosure=Full quests=1", outcome.QuestSummary);
        var system = llm.LastMessages![0].Content;
        Assert.Contains("在海关找到被藏在宿舍楼里的水", system, StringComparison.Ordinal);
        Assert.Contains("详细描述文本", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_QuestsPlaceholder_IsReplaced()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var snapshot = new TraderRelationshipSnapshot(
            LoyaltyLevel: 2,
            ActiveQuestDetails: [new TraderQuestDetail("水瓶座行动 - 1", [], null, "海关")]);
        var engine = BuildEngine(
            llm: llm,
            templates: new StubPromptTemplateProvider("P={{persona}}\nQ={{quests}}"),
            relationship: new StubRelationshipProvider(snapshot));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        var system = llm.LastMessages![0].Content;
        Assert.DoesNotContain("{{quests}}", system, StringComparison.Ordinal);
        Assert.Contains("当前进行中的委托", system, StringComparison.Ordinal);
        Assert.Contains("水瓶座行动 - 1", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_LegacyTemplateWithoutQuests_AppendsSection_WithoutReExpansion()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var snapshot = new TraderRelationshipSnapshot(
            LoyaltyLevel: 2,
            ActiveQuestDetails: [new TraderQuestDetail("A{{knowledge}}B", [], null, null)]);
        var engine = BuildEngine(
            llm: llm,
            templates: new StubPromptTemplateProvider("{{persona}}\n{{knowledge}}"),
            retrieverFactory: new StubRetrieverFactory([new KnowledgeSnippet("faq.md", "保险", "SNIPPET_TEXT", 5)]),
            relationship: new StubRelationshipProvider(snapshot));

        await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        var system = llm.LastMessages![0].Content;
        // 委托段自动追加进 persona；其内容里的 {{knowledge}} 字面量不得二次展开。
        Assert.Contains("A{{knowledge}}B", system, StringComparison.Ordinal);
        Assert.Contains("当前进行中的委托", system, StringComparison.Ordinal);
        Assert.Contains("SNIPPET_TEXT", system, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HandleMessage_NoActiveQuestDetails_InjectsDirectiveOnly()
    {
        var llm = new RecordingLlmClient(LlmCompletionResult.Ok("ok"));
        var engine = BuildEngine(llm: llm, relationship: new StubRelationshipProvider(null));

        var outcome = await engine.HandleMessageAsync(SessionId, TraderId, "hi");

        Assert.Equal("Disclosure=None quests=0", outcome.QuestSummary);
        var system = llm.LastMessages![0].Content;
        // 修订 R1：无委托时不再空段，反编造禁令仍注入；但不出现委托段标题。
        Assert.DoesNotContain("当前进行中的委托", system, StringComparison.Ordinal);
        Assert.Contains("严禁编造", system, StringComparison.Ordinal);
    }

    private sealed class RecordingLlmClient(LlmCompletionResult result) : ILlmClient
    {
        public IReadOnlyList<LlmChatMessage>? LastMessages { get; private set; }

        public bool WasCalled { get; private set; }

        public Task<LlmCompletionResult> CompleteAsync(
            IReadOnlyList<LlmChatMessage> messages, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastMessages = messages;
            return Task.FromResult(result);
        }
    }

    private sealed class StubPromptTemplateProvider(string template) : IPromptTemplateProvider
    {
        public PromptTemplateResult GetSystemPrompt(string relativePath) => new(template, false, null);
    }

    private sealed class StubRetrieverFactory(IReadOnlyList<KnowledgeSnippet> snippets) : IKnowledgeRetrieverFactory
    {
        public string? LastQuery { get; private set; }

        public int LastTopK { get; private set; }

        public IKnowledgeRetriever Create(string docsDirectory) => new StubRetriever(snippets, this);

        private sealed class StubRetriever(IReadOnlyList<KnowledgeSnippet> snippets, StubRetrieverFactory owner)
            : IKnowledgeRetriever
        {
            public Task<IReadOnlyList<KnowledgeSnippet>> RetrieveAsync(
                string query, int topK, CancellationToken cancellationToken = default)
            {
                owner.LastQuery = query;
                owner.LastTopK = topK;
                return Task.FromResult(snippets);
            }
        }
    }

    private sealed class CancellingRetrieverFactory : IKnowledgeRetrieverFactory
    {
        public IKnowledgeRetriever Create(string docsDirectory) => new CancellingRetriever();

        private sealed class CancellingRetriever : IKnowledgeRetriever
        {
            public Task<IReadOnlyList<KnowledgeSnippet>> RetrieveAsync(
                string query, int topK, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult<IReadOnlyList<KnowledgeSnippet>>([]);
            }
        }
    }

    private sealed class InMemoryHistoryProvider : IChatHistoryProvider
    {
        private readonly Dictionary<string, List<ChatHistoryMessage>> _store = new(StringComparer.Ordinal);

        public int LastRequestedCount { get; private set; }

        public void Seed(string sessionId, string traderId, params ChatHistoryMessage[] messages) =>
            _store[$"{sessionId}|{traderId}"] = [.. messages];

        public Task<IReadOnlyList<ChatHistoryMessage>> GetRecentAsync(
            string sessionId, string traderId, int count, CancellationToken cancellationToken = default)
        {
            LastRequestedCount = count;
            IReadOnlyList<ChatHistoryMessage> result = _store.TryGetValue($"{sessionId}|{traderId}", out var all)
                ? all.TakeLast(count).ToList()
                : [];
            return Task.FromResult(result);
        }
    }

    private sealed class InMemoryStateStore : ITraderChatStateStore
    {
        private readonly HashSet<string> _opened = new(StringComparer.Ordinal);
        private readonly object _gate = new();

        public Task<bool> TryClaimOpeningAsync(
            string sessionId, string traderId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(_opened.Add($"{sessionId}|{traderId}"));
            }
        }

        public Task<bool> IsOpeningSentAsync(
            string sessionId, string traderId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                return Task.FromResult(_opened.Contains($"{sessionId}|{traderId}"));
            }
        }

        public Task MarkOpeningSentAsync(
            string sessionId, string traderId, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                _opened.Add($"{sessionId}|{traderId}");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class StubRelationshipProvider(TraderRelationshipSnapshot? snapshot) : ITraderRelationshipProvider
    {
        public Task<TraderRelationshipSnapshot?> GetRelationshipAsync(
            string sessionId, string traderId, CancellationToken cancellationToken = default) =>
            Task.FromResult(snapshot);
    }

    private sealed class ThrowingRelationshipProvider(string message) : ITraderRelationshipProvider
    {
        public Task<TraderRelationshipSnapshot?> GetRelationshipAsync(
            string sessionId, string traderId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException(message);
    }

    private sealed class CancellingRelationshipProvider : ITraderRelationshipProvider
    {
        public Task<TraderRelationshipSnapshot?> GetRelationshipAsync(
            string sessionId, string traderId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<TraderRelationshipSnapshot?>(null);
        }
    }
}

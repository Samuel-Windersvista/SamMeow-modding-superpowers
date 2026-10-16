using System.Text;
using System.Text.RegularExpressions;
using SamMeow.LLMTraderChat.Server.Config;
using SamMeow.LLMTraderChat.Server.Knowledge;
using SamMeow.LLMTraderChat.Server.Llm;

namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 聊天引擎（主接缝）：编排 reset 指令识别 → 开场一次性判定 → 检索 → prompt 组装 → LLM 调用 → 失败兜底。
///
/// 不依赖 SPT 运行时类型，全部依赖经构造函数以接口注入；不做 SPT 接线（属工单 07）。
/// 延迟秒数在配置范围内取值（见 <see cref="ModConfig.ReplyDelay"/>）；数值参数的范围钳制由装载层
/// （<c>ConfigLoader</c>）单一负责，本引擎按配置值直接使用。
/// </summary>
public sealed class TraderChatEngine
{
    /// <summary>LLM 未配置/未就绪时返回给玩家的明确提示（不静默）。</summary>
    public const string NotReadyMessage = "（商人聊天尚未就绪：请在服务端 config.jsonc 配置 LLM 端点与模型后重试。）";

    // ---- prompt 尺寸预算（先用常数，暂不引入配置字段，后续按需提升为配置） ----
    // 取值依据：默认 historyMessageCount=20、knowledgeTopK=4。按单条约 200 字、单片段约 750 字估算，
    // 约束 system prompt 不无界膨胀，同时为 persona 与行为约束留足空间；单条上限防止超长单条挤占其余内容。
    private const int MaxHistoryItemChars = 500;
    private const int MaxHistoryTotalChars = 4000;
    private const int MaxKnowledgeItemChars = 800;
    private const int MaxKnowledgeTotalChars = 3000;

    private static readonly Regex PromptPlaceholderRegex = new(
        @"\{\{(persona|attitude|quests|knowledge|history)\}\}", RegexOptions.Compiled);

    private readonly ModConfig _config;
    private readonly ILlmClient _llmClient;
    private readonly IKnowledgeRetrieverFactory _retrieverFactory;
    private readonly IChatHistoryProvider _historyProvider;
    private readonly ITraderChatStateStore _stateStore;
    private readonly IPromptTemplateProvider _promptTemplates;
    private readonly ITraderRelationshipProvider _relationshipProvider;

    /// <summary>构造引擎（全部依赖经接口注入）。关系提供器可省略（默认空实现，降级 Cold 档）。</summary>
    public TraderChatEngine(
        ModConfig config,
        ILlmClient llmClient,
        IKnowledgeRetrieverFactory retrieverFactory,
        IChatHistoryProvider historyProvider,
        ITraderChatStateStore stateStore,
        IPromptTemplateProvider promptTemplates,
        ITraderRelationshipProvider? relationshipProvider = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
        _retrieverFactory = retrieverFactory ?? throw new ArgumentNullException(nameof(retrieverFactory));
        _historyProvider = historyProvider ?? throw new ArgumentNullException(nameof(historyProvider));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _promptTemplates = promptTemplates ?? throw new ArgumentNullException(nameof(promptTemplates));
        _relationshipProvider = relationshipProvider ?? NullTraderRelationshipProvider.Instance;
    }

    /// <summary>
    /// 处理一条玩家发往某商人的消息。商人未启用 / 未知 / 空文本 → <see cref="ChatOutcomeKind.Ignored"/>。
    /// </summary>
    public async Task<TraderChatOutcome> HandleMessageAsync(
        string sessionId,
        string traderId,
        string playerText,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetEnabledTrader(traderId, out var trader) || string.IsNullOrWhiteSpace(playerText))
        {
            return TraderChatOutcome.Ignored();
        }

        if (IsResetCommand(playerText, trader))
        {
            var confirmation = string.IsNullOrWhiteSpace(_config.ResetConfirmationMessage)
                ? string.Empty
                : _config.ResetConfirmationMessage;
            return TraderChatOutcome.Reset(confirmation, confirmation.Length > 0 ? NextDelaySeconds() : 0);
        }

        var warnings = new List<string>();

        var snippets = await RetrieveSnippetsAsync(trader, playerText, warnings, cancellationToken).ConfigureAwait(false);
        var knowledgeSources = snippets
            .Select(snippet => snippet.Source)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var history = await _historyProvider
            .GetRecentAsync(sessionId, traderId, _config.HistoryMessageCount, cancellationToken)
            .ConfigureAwait(false);

        var template = _promptTemplates.GetSystemPrompt(_config.SystemPromptPath);
        if (template.Warning is not null)
        {
            warnings.Add(template.Warning);
        }

        var relationship = await RetrieveRelationshipAsync(sessionId, traderId, warnings, cancellationToken).ConfigureAwait(false);
        var attitudeTier = TraderAttitudeEvaluator.EvaluateTier(relationship);
        var attitudeSummary = TraderAttitudeEvaluator.FormatDiagnosticSummary(attitudeTier, relationship);
        var attitudeBlock = TraderAttitudeEvaluator.RenderAttitudeBlock(relationship, trader, warnings);

        var disclosure = DisclosurePolicy.FromAttitudeTier(attitudeTier);
        var questBlock = TraderQuestRenderer.RenderQuestBlock(relationship?.ActiveQuestDetails, disclosure, warnings);
        var disclosedQuestCount = disclosure == DisclosureLevel.None
            ? 0
            : relationship?.ActiveQuestDetails?.Count ?? 0;
        var questSummary = $"Disclosure={disclosure} quests={disclosedQuestCount}";

        var systemPrompt = RenderSystemPrompt(template.Template, trader, snippets, history, attitudeBlock, questBlock, warnings);
        var messages = new List<LlmChatMessage>
        {
            LlmChatMessage.System(systemPrompt),
            LlmChatMessage.User(playerText),
        };

        var completion = await _llmClient.CompleteAsync(messages, cancellationToken).ConfigureAwait(false);

        if (completion.Success && !string.IsNullOrWhiteSpace(completion.Content))
        {
            return new TraderChatOutcome(
                ChatOutcomeKind.Reply, completion.Content.Trim(), NextDelaySeconds(), false, warnings,
                snippets.Count, knowledgeSources, attitudeTier, attitudeSummary, questSummary);
        }

        if (completion.FailureKind == LlmFailureKind.NotConfigured)
        {
            if (completion.ErrorDetail is not null)
            {
                warnings.Add(completion.ErrorDetail);
            }

            return new TraderChatOutcome(
                ChatOutcomeKind.NotReady, NotReadyMessage, NextDelaySeconds(), false, warnings,
                snippets.Count, knowledgeSources, attitudeTier, attitudeSummary, questSummary);
        }

        // 其余失败（HttpError / Timeout / InvalidResponse / 空内容）→ 兜底话术。
        // 配置留空时回落内置默认话术（避免给玩家发空消息）。
        warnings.Add(completion.ErrorDetail
            ?? (completion.Success ? "LLM 返回空内容" : "LLM 调用失败"));
        var fallbackMessage = string.IsNullOrWhiteSpace(_config.FallbackMessage)
            ? ModConfig.DefaultFallbackMessage
            : _config.FallbackMessage;
        return new TraderChatOutcome(
            ChatOutcomeKind.Fallback, fallbackMessage, NextDelaySeconds(), false, warnings,
            snippets.Count, knowledgeSources, attitudeTier, attitudeSummary, questSummary);
    }

    /// <summary>
    /// 取该商人的一次性开场消息：未启用 / 未配置开场文案 / 已发过 → <see cref="ChatOutcomeKind.Ignored"/>。
    /// 首次调用返回 <see cref="ChatOutcomeKind.Opening"/> 并标记已发。
    ///
    /// 语义为 **at-most-once**：实现「先标记后返回」，若接线发送失败也不会重发；是否改为原子 claim
    /// 由接线工单 07 决定（本接口不承载该策略）。
    /// </summary>
    public async Task<TraderChatOutcome> GetOpeningMessageAsync(
        string sessionId,
        string traderId,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetEnabledTrader(traderId, out var trader) || string.IsNullOrWhiteSpace(trader.OpeningMessage))
        {
            return TraderChatOutcome.Ignored();
        }

        // 原子认领：并发两路只有一路拿到开场资格（消除双发窗口）。
        if (!await _stateStore.TryClaimOpeningAsync(sessionId, traderId, cancellationToken).ConfigureAwait(false))
        {
            return TraderChatOutcome.Ignored();
        }

        return new TraderChatOutcome(ChatOutcomeKind.Opening, trader.OpeningMessage, NextDelaySeconds(), false, []);
    }

    private bool TryGetEnabledTrader(string traderId, out TraderConfig trader)
    {
        trader = _config.Traders.FirstOrDefault(
            candidate => candidate.Enabled && string.Equals(candidate.TraderId, traderId, StringComparison.Ordinal))!;
        return trader is not null;
    }

    private static bool IsResetCommand(string text, TraderConfig trader)
    {
        var normalized = text.Trim();
        foreach (var keyword in trader.ResetKeywords)
        {
            if (keyword.Length > 0 && string.Equals(normalized, keyword.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<IReadOnlyList<KnowledgeSnippet>> RetrieveSnippetsAsync(
        TraderConfig trader, string query, List<string> warnings, CancellationToken cancellationToken)
    {
        try
        {
            var retriever = _retrieverFactory.Create(trader.DocsDir);
            return await retriever.RetrieveAsync(query, _config.KnowledgeTopK, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"[LLMTC][engine] 知识检索失败，已跳过片段注入：{ex.Message}");
            return [];
        }
    }

    private async Task<TraderRelationshipSnapshot?> RetrieveRelationshipAsync(
        string sessionId, string traderId, List<string> warnings, CancellationToken cancellationToken)
    {
        try
        {
            return await _relationshipProvider
                .GetRelationshipAsync(sessionId, traderId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            warnings.Add($"[LLMTC][engine] 玩家关系状态读取失败，已回退默认态度：{ex.Message}");
            return null;
        }
    }

    private double NextDelaySeconds()
    {
        var min = _config.ReplyDelay.MinSeconds;
        var max = _config.ReplyDelay.MaxSeconds;
        if (max <= min)
        {
            return Math.Max(0, min);
        }

        return min + (Random.Shared.NextDouble() * (max - min));
    }

    private static string RenderSystemPrompt(
        string template,
        TraderConfig trader,
        IReadOnlyList<KnowledgeSnippet> snippets,
        IReadOnlyList<ChatHistoryMessage> history,
        string attitudeBlock,
        string questBlock,
        List<string> warnings)
    {
        var hasPersona = template.Contains("{{persona}}", StringComparison.Ordinal);
        var hasAttitude = template.Contains("{{attitude}}", StringComparison.Ordinal);
        var hasQuests = template.Contains("{{quests}}", StringComparison.Ordinal);

        // 态度/任务块无占位符时靠 persona 兜底追加；persona 也缺则对应块会静默丢失。
        if (!hasPersona && (!hasAttitude || !hasQuests))
        {
            warnings.Add("[LLMTC][engine] 自定义 system prompt 模板缺少 {{persona}}/{{attitude}}/{{quests}} 占位符，态度或任务块未注入");
        }

        var persona = string.IsNullOrWhiteSpace(trader.Persona) ? "（未设定）" : trader.Persona;
        var personaValue = new StringBuilder(persona);
        if (!hasAttitude)
        {
            personaValue.Append("\n当前对玩家的态度与关系背景：\n").Append(attitudeBlock);
        }

        if (!hasQuests && questBlock.Length > 0)
        {
            personaValue.Append('\n').Append(questBlock);
        }

        var values = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            // 含 {{attitude}}/{{quests}} 的模板直接展开；旧模板把对应块拼入 persona，保证向后兼容。
            ["{{persona}}"] = personaValue.ToString(),
            ["{{attitude}}"] = attitudeBlock,
            ["{{quests}}"] = questBlock,
            ["{{knowledge}}"] = RenderKnowledge(snippets, warnings),
            ["{{history}}"] = RenderHistory(history, warnings),
        };

        // 单趟替换：只扫描原模板，替换值的 {{...}} 字面量不会被二次展开。
        return PromptPlaceholderRegex.Replace(
            template, match => values.TryGetValue(match.Value, out var value) ? value : match.Value);
    }

    private static string RenderKnowledge(IReadOnlyList<KnowledgeSnippet> snippets, List<string> warnings)
    {
        if (snippets.Count == 0)
        {
            return "（无相关知识片段）";
        }

        var builder = new StringBuilder();
        var totalChars = 0;
        var perItemTruncated = 0;
        var dropped = 0;

        for (var index = 0; index < snippets.Count; index++)
        {
            var snippet = snippets[index];
            var text = snippet.Text ?? string.Empty;
            if (text.Length > MaxKnowledgeItemChars)
            {
                text = Truncate(text, MaxKnowledgeItemChars);
                perItemTruncated++;
            }

            var block = $"[{index + 1}] 来源：{snippet.Source}｜标题：{snippet.Title}\n{text}";
            var separator = builder.Length > 0 ? 1 : 0;
            if (totalChars + separator + block.Length > MaxKnowledgeTotalChars)
            {
                dropped = snippets.Count - index; // 本条及后续（相关度更低）因总量预算丢弃
                break;
            }

            if (separator > 0)
            {
                builder.Append('\n');
            }

            builder.Append(block);
            totalChars += separator + block.Length;
        }

        if (perItemTruncated > 0)
        {
            warnings.Add($"[LLMTC][engine] 知识渲染截断：{perItemTruncated} 条超出单条预算 {MaxKnowledgeItemChars} 字已截断");
        }

        if (dropped > 0)
        {
            warnings.Add($"[LLMTC][engine] 知识渲染截断：{dropped} 条超出总量预算 {MaxKnowledgeTotalChars} 字未注入（已注入 {totalChars} 字）");
        }

        return builder.ToString();
    }

    private static string RenderHistory(IReadOnlyList<ChatHistoryMessage> history, List<string> warnings)
    {
        if (history.Count == 0)
        {
            return "（无）";
        }

        // 总量超预算时优先保留最近的消息：history 为旧→新，自后向前累积后再反序。
        var lines = new List<string>(history.Count);
        var totalChars = 0;
        var perItemTruncated = 0;
        var dropped = 0;

        for (var index = history.Count - 1; index >= 0; index--)
        {
            var speaker = history[index].Role == ChatRole.Player ? "玩家" : "商人";
            var text = history[index].Text ?? string.Empty;
            if (text.Length > MaxHistoryItemChars)
            {
                text = Truncate(text, MaxHistoryItemChars);
                perItemTruncated++;
            }

            var line = $"{speaker}：{text}";
            var separator = lines.Count > 0 ? 1 : 0;
            if (totalChars + separator + line.Length > MaxHistoryTotalChars)
            {
                dropped = index + 1; // 更旧的全部丢弃
                break;
            }

            lines.Add(line);
            totalChars += separator + line.Length;
        }

        lines.Reverse();

        if (perItemTruncated > 0)
        {
            warnings.Add($"[LLMTC][engine] 历史渲染截断：{perItemTruncated} 条超出单条预算 {MaxHistoryItemChars} 字已截断");
        }

        if (dropped > 0)
        {
            warnings.Add($"[LLMTC][engine] 历史渲染截断：{dropped} 条超出总量预算 {MaxHistoryTotalChars} 字未注入（已注入 {totalChars} 字）");
        }

        return string.Join('\n', lines);
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength];
}

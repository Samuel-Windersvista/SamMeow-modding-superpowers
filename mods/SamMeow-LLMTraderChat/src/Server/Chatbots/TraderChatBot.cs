using System.Diagnostics;
using SamMeow.LLMTraderChat.Server.Engine;
using SamMeow.LLMTraderChat.Server.Spt;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;

namespace SamMeow.LLMTraderChat.Server.Chatbots;

/// <summary>
/// 引擎驱动的商人 chatbot（路线 A）：每个启用的商人一个实例，经 DI 注册为
/// <see cref="IDialogueChatBot"/>；<c>DialogueController.SendMessage</c> 按 <c>request.DialogId</c>
/// 路由到对应实例。
///
/// 路由线程不阻塞：<see cref="HandleMessage"/> 立即返回，真正的「引擎调用 → 延迟 → 发送」在后台任务
/// 执行（含异常与告警的响亮日志）。玩家消息已由 <c>DialogueController</c> 在处理前落库，历史读取可见。
/// </summary>
public sealed class TraderChatBot : IDialogueChatBot
{
    private readonly string _traderId;
    private readonly string _traderName;
    private readonly TraderChatEngine _engine;
    private readonly MailSendService _mailSendService;
    private readonly DialogueHelper _dialogueHelper;
    private readonly ISptLogger<TraderChatBot> _logger;

    /// <summary>构造（由 DI 工厂按商人创建）。</summary>
    public TraderChatBot(
        string traderId,
        string traderName,
        TraderChatEngine engine,
        MailSendService mailSendService,
        DialogueHelper dialogueHelper,
        ISptLogger<TraderChatBot> logger)
    {
        _traderId = traderId;
        _traderName = traderName;
        _engine = engine;
        _mailSendService = mailSendService;
        _dialogueHelper = dialogueHelper;
        _logger = logger;
    }

    /// <inheritdoc />
    public UserDialogInfo GetChatBot()
    {
        return new UserDialogInfo
        {
            Id = new MongoId(_traderId),
            Aid = 0,
            Info = new UserDialogDetails
            {
                Nickname = _traderName,
                Side = "Usec",
                Level = 1,
                MemberCategory = MemberCategory.Trader,
                SelectedMemberCategory = MemberCategory.Trader,
            },
        };
    }

    /// <inheritdoc />
    public ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
    {
        _logger.Info(
            $"[LLMTC][recv] 商人={_traderName} session={sessionId} dialogId={request.DialogId} text=\"{request.Text}\"");
        _logger.Info($"[LLMTC][route] bot={_traderName} 接管 dialogId={request.DialogId}");

        // 后台处理：不阻塞路由线程（HTTP 响应立即返回）。
        _ = ProcessAsync(sessionId, request.Text);
        return new ValueTask<string>(request.DialogId);
    }

    private async Task ProcessAsync(MongoId sessionId, string playerText)
    {
        try
        {
            var started = Stopwatch.GetTimestamp();
            var outcome = await _engine.HandleMessageAsync(sessionId.ToString(), _traderId, playerText).ConfigureAwait(false);
            var elapsedMs = (long)((Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency);

            foreach (var warning in outcome.Warnings)
            {
                _logger.Warning($"[LLMTC] {warning}");
            }

            if (outcome.Kind == ChatOutcomeKind.Ignored)
            {
                _logger.Info($"[LLMTC][route] 无动作（ignored） 商人={_traderName} session={sessionId}");
                return;
            }

            var knowledgeSources = outcome.KnowledgeSources is { Count: > 0 } sources
                ? string.Join(",", sources)
                : "无";
            _logger.Info($"[LLMTC][kg] 命中数={outcome.KnowledgeHitCount} 来源={knowledgeSources} 商人={_traderName}");
            if (!string.IsNullOrWhiteSpace(outcome.AttitudeSummary))
            {
                _logger.Info($"[LLMTC][attitude] 档位={outcome.AttitudeSummary} 商人={_traderName}");
            }

            if (!string.IsNullOrWhiteSpace(outcome.QuestSummary))
            {
                _logger.Info($"[LLMTC][quests] {outcome.QuestSummary} 商人={_traderName}");
            }

            _logger.Info($"[LLMTC][engine] 耗时={elapsedMs}ms 结果={outcome.Kind} 商人={_traderName}");

            if (outcome.Kind == ChatOutcomeKind.Reset)
            {
                ClearDialogue(sessionId);
            }

            if (string.IsNullOrWhiteSpace(outcome.Text))
            {
                if (outcome.Kind is ChatOutcomeKind.Fallback or ChatOutcomeKind.NotReady)
                {
                    _logger.Warning($"[LLMTC] {outcome.Kind} 无可用文案，跳过发送 商人={_traderName}");
                }

                return;
            }

            if (outcome.DelaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(outcome.DelaySeconds)).ConfigureAwait(false);
            }

            _mailSendService.SendDirectNpcMessageToPlayer(
                sessionId, _traderId, MessageType.NpcTraderMessage, outcome.Text, null);

            switch (outcome.Kind)
            {
                case ChatOutcomeKind.Fallback:
                    _logger.Warning($"[LLMTC][fallback] 已发送兜底话术 商人={_traderName} session={sessionId}");
                    break;
                case ChatOutcomeKind.NotReady:
                    _logger.Warning($"[LLMTC][notready] 已发送未就绪提示 商人={_traderName} session={sessionId}");
                    break;
                default:
                    _logger.Info($"[LLMTC][reply] 商人={_traderName} session={sessionId} 已发送 {outcome.Text.Length} 字");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"[LLMTC][reply] 处理消息异常 商人={_traderName} session={sessionId}: {ex}");
        }
    }

    /// <summary>
    /// reset 的选择性清空：只移除**无附件**消息（即对话记忆），保留带附件消息（未领取保险/任务奖励），
    /// 避免误删玩家尚未领取的物品；同时清零 `New`、按保留的未领取附件重算 `AttachmentsNew`（消除幽灵角标）。
    /// 随后若配置了确认文案，会重新写入一条商人消息，使对话保持可见。
    /// </summary>
    private void ClearDialogue(MongoId sessionId)
    {
        var dialogues = _dialogueHelper.GetDialogsForProfile(sessionId);
        if (!dialogues.TryGetValue(_traderId, out var dialogue) || dialogue.Messages is not { } messages)
        {
            return;
        }

        var removed = messages.RemoveAll(
            message => DialogueResetPolicy.ShouldRemoveOnReset(message.Items?.Data?.Count ?? 0));
        dialogue.New = 0;
        dialogue.AttachmentsNew = messages.Count(
            message => message.HasRewards.GetValueOrDefault(false) && !message.RewardCollected.GetValueOrDefault(false));

        _logger.Info(
            $"[LLMTC][reset] 选择性清空：移除 {removed} 条无附件消息，保留 {messages.Count} 条（含附件）"
            + $" 商人={_traderName} session={sessionId}");
    }
}

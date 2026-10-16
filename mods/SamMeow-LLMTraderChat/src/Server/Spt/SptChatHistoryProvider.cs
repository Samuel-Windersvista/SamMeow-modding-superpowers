using SamMeow.LLMTraderChat.Server.Engine;
using SPTarkov.Server.Core.Helpers.Profile;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace SamMeow.LLMTraderChat.Server.Spt;

/// <summary>
/// 从 SPT profile 对话读取历史的 <see cref="IChatHistoryProvider"/> 生产实现。
///
/// 形态（源码实读）：<c>SptProfile.DialogueRecords[traderId].Messages</c> 持久化到 profile；
/// 玩家消息 <c>MessageType.UserMessage</c>（<c>MailSendService.SendPlayerMessageToNpc</c> 写入，
/// 在 <c>DialogueController.SendMessage</c> 调用 bot <b>之前</b>），商人消息为其余非空文本类型
/// （<c>NpcTraderMessage</c> / <c>MessageWithItems</c> 等）。
///
/// 因当前玩家消息总在本次处理前已落库并位于末尾，读取时排除末尾玩家消息，避免与引擎显式加入的
/// 当前 user turn 重复。只读；写入/清空由接线（bot）负责。
/// </summary>
public sealed class SptChatHistoryProvider : IChatHistoryProvider
{
    private readonly DialogueHelper _dialogueHelper;

    /// <summary>构造。</summary>
    public SptChatHistoryProvider(DialogueHelper dialogueHelper)
    {
        _dialogueHelper = dialogueHelper;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ChatHistoryMessage>> GetRecentAsync(
        string sessionId,
        string traderId,
        int count,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return Task.FromResult<IReadOnlyList<ChatHistoryMessage>>([]);
        }

        var dialogues = _dialogueHelper.GetDialogsForProfile(new MongoId(sessionId));
        if (!dialogues.TryGetValue(traderId, out var dialogue) || dialogue.Messages is not { Count: > 0 } messages)
        {
            return Task.FromResult<IReadOnlyList<ChatHistoryMessage>>([]);
        }

        var mapped = new List<ChatHistoryMessage>(messages.Count);
        foreach (var message in messages)
        {
            var text = message.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var role = message.MessageType == MessageType.UserMessage ? ChatRole.Player : ChatRole.Trader;
            mapped.Add(new ChatHistoryMessage(role, text));
        }

        // 排除末尾的「当前玩家消息」（见类型注释）。
        if (mapped.Count > 0 && mapped[^1].Role == ChatRole.Player)
        {
            mapped.RemoveAt(mapped.Count - 1);
        }

        if (mapped.Count > count)
        {
            mapped = mapped.GetRange(mapped.Count - count, count);
        }

        return Task.FromResult<IReadOnlyList<ChatHistoryMessage>>(mapped);
    }
}

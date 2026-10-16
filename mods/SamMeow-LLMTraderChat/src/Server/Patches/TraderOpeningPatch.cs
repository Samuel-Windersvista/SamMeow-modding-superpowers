using System.Reflection;
using SamMeow.LLMTraderChat.Server.Config;
using SamMeow.LLMTraderChat.Server.Engine;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;

namespace SamMeow.LLMTraderChat.Server.Patches;

/// <summary>
/// 开场消息接线（替代 M1 的「补发固定消息」seed 补丁）。
///
/// 目标方法：<c>DialogueController.GenerateDialogueList(MongoId)</c>（<c>/client/mail/dialog/list</c>
/// 入口，玩家打开 Messenger 时触发）。Prefix 遍历启用商人，对每个商人调用
/// <see cref="TraderChatEngine.GetOpeningMessageAsync"/>；首次（且配置了开场文案）即延迟后经
/// <c>MailSendService</c> 发送——同时创建对话（对话不存在时 <c>SendMessageToPlayer</c> 会自动建），
/// 沿用 M1 验证过的「补发创建」形态。一次性由引擎的状态存储保证（跨重启 at-most-once）。
///
/// 发送为后台任务（不阻塞列表生成）；延迟期间列表本次可能不含该对话，下次刷新出现。
/// </summary>
public sealed class TraderOpeningPatch : AbstractPatch
{
    private static TraderChatEngine _engine = null!;
    private static MailSendService _mailSendService = null!;
    private static ModConfig _config = null!;
    private static ISptLogger<TraderOpeningPatch> _logger = null!;

    /// <summary>构造（由 ModEntry 在 OnLoad 时创建并 Enable）。</summary>
    public TraderOpeningPatch(
        TraderChatEngine engine,
        MailSendService mailSendService,
        ModConfig config,
        ISptLogger<TraderOpeningPatch> logger)
    {
        _engine = engine;
        _mailSendService = mailSendService;
        _config = config;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override MethodBase? GetTargetMethod()
    {
        return typeof(DialogueController).GetMethod(
            nameof(DialogueController.GenerateDialogueList),
            BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPrefix]
    private static void Prefix(MongoId sessionId)
    {
        foreach (var trader in _config.Traders)
        {
            if (trader.Enabled)
            {
                _ = SendOpeningAsync(sessionId, trader);
            }
        }
    }

    private static async Task SendOpeningAsync(MongoId sessionId, TraderConfig trader)
    {
        try
        {
            var outcome = await _engine.GetOpeningMessageAsync(sessionId.ToString(), trader.TraderId).ConfigureAwait(false);
            if (outcome.Kind != ChatOutcomeKind.Opening || string.IsNullOrWhiteSpace(outcome.Text))
            {
                return;
            }

            if (outcome.DelaySeconds > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(outcome.DelaySeconds)).ConfigureAwait(false);
            }

            _mailSendService.SendDirectNpcMessageToPlayer(
                sessionId, trader.TraderId, MessageType.NpcTraderMessage, outcome.Text, null);
            var traderName = string.IsNullOrWhiteSpace(trader.Name) ? trader.TraderId : trader.Name;
            _logger.Info($"[LLMTC][opening] 商人={traderName} session={sessionId} 已发送开场消息");
        }
        catch (Exception ex)
        {
            _logger.Error($"[LLMTC][opening] 发送开场消息异常 商人={trader.TraderId} session={sessionId}: {ex}");
        }
    }
}

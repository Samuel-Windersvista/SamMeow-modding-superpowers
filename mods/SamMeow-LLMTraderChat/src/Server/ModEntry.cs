using SamMeow.LLMTraderChat.Server.Config;
using SamMeow.LLMTraderChat.Server.Engine;
using SamMeow.LLMTraderChat.Server.Patches;
using SamMeow.LLMTraderChat.Shared;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Services.Commerce;

namespace SamMeow.LLMTraderChat.Server;

/// <summary>
/// 服务端加载钩子（M2）：记录配置与已接线商人；启用「首次开场消息」补丁。
/// chatbot 与引擎依赖由 <see cref="ConfigRegistration"/>（IOnDIConstruct）注册。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public sealed class ModEntry(
    ISptLogger<ModEntry> logger,
    ISptLogger<TraderOpeningPatch> openingLogger,
    MailSendService mailSendService,
    ModConfig config,
    ConfigLoadResult configResult,
    TraderChatEngine engine) : IOnLoad
{
    /// <summary>固定启动日志行，供实机部署阶段 grep 核验。</summary>
    public static readonly string LoadLogLine =
        $"[LLMTC] {SharedConstants.ModName} v{ModVersion.Value} 服务端已加载";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info(LoadLogLine);

        // 配置装载告警：逐条响亮输出（含来源文件与字段）。
        foreach (var warning in configResult.Warnings)
        {
            logger.Warning(warning);
        }

        var enabledTraders = config.Traders.Where(trader => trader.Enabled).ToList();
        logger.Info(
            $"[LLMTC][config] 引擎已装载：LLM 端点={config.Llm.Endpoint} 模型={config.Llm.Model} "
            + $"超时={config.Llm.TimeoutSeconds}s；延迟={config.ReplyDelay.MinSeconds}-{config.ReplyDelay.MaxSeconds}s；"
            + $"历史={config.HistoryMessageCount} topK={config.KnowledgeTopK}；"
            + $"启用商人={enabledTraders.Count}/{config.Traders.Count}");

        foreach (var trader in enabledTraders)
        {
            var traderName = string.IsNullOrWhiteSpace(trader.Name) ? trader.TraderId : trader.Name;
            logger.Info($"[LLMTC][route] 已接线商人：{traderName}（{trader.TraderId}）");
        }

        // 开场消息接线：GenerateDialogueList 前缀 → 首次（且配置了文案）发开场并创建对话。
        // AbstractPatch 的所有权检查要求构造与 Enable 同在本 mod 程序集（官方示例同款）。
        new TraderOpeningPatch(engine, mailSendService, config, openingLogger).Enable();
        logger.Info("[LLMTC][opening] 已启用「首次开场消息」补丁（GenerateDialogueList prefix）");

        return Task.CompletedTask;
    }
}

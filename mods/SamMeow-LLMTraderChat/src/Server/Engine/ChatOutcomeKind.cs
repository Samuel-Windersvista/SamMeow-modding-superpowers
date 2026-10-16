namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>引擎对一次输入的处理结果类别，供接线（07）决定后续动作（发消息 / 清历史 / 不动作）。</summary>
public enum ChatOutcomeKind
{
    /// <summary>无动作（商人未启用 / 未知商人 / 空文本 / 开场已发）。</summary>
    Ignored = 0,

    /// <summary>正常回复（LLM 成功）。</summary>
    Reply,

    /// <summary>LLM 失败 → 返回配置的兜底话术。</summary>
    Fallback,

    /// <summary>LLM 未配置/未就绪 → 返回明确的未就绪提示（不静默）。</summary>
    NotReady,

    /// <summary>识别到 reset 指令：请求接线清空该商人对话历史（<see cref="TraderChatOutcome.ResetHistory"/> 为 true）。</summary>
    Reset,

    /// <summary>一次性开场消息。</summary>
    Opening,
}

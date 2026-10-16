using System.Text.Json.Serialization;

namespace SamMeow.LLMTraderChat.Server.Config;

/// <summary>
/// 全局 + per-trader 配置（纯 POCO）。
///
/// STD-CFG-004：配置类禁止标注 [Injectable]——否则容器会用默认值新建实例，磁盘上的
/// config.jsonc 永远不会被读取；本类型只能由 <see cref="ConfigRegistration"/> 经
/// IOnDIConstruct 读盘后 AddSingleton 注册（STD-CFG-003）。
/// 键名与 config/config.jsonc 一一对应（由单测守护）。
/// </summary>
public sealed class ModConfig
{
    /// <summary>兜底话术默认值（LLM 不可用或无有效回复时返回给玩家）。</summary>
    public const string DefaultFallbackMessage = "（商人暂时联系不上，请稍后再试。）";

    /// <summary>reset 确认文案默认值（清空历史后回给玩家；空的表示不发送确认）。</summary>
    public const string DefaultResetConfirmationMessage = "（对话记忆已重置。我们重新开始吧。）";

    /// <summary>LLM 端点与参数。</summary>
    [JsonPropertyName("llm")]
    public LlmConfig Llm { get; set; } = new();

    /// <summary>回复延迟范围（秒，区间 [min, max) 内随机）。</summary>
    [JsonPropertyName("replyDelay")]
    public ReplyDelayConfig ReplyDelay { get; set; } = new();

    /// <summary>构造 prompt 时注入的最近对话条数。</summary>
    [JsonPropertyName("historyMessageCount")]
    public int HistoryMessageCount { get; set; } = 20;

    /// <summary>检索时取用的知识片段条数（top-k）。</summary>
    [JsonPropertyName("knowledgeTopK")]
    public int KnowledgeTopK { get; set; } = 4;

    /// <summary>兜底话术。</summary>
    [JsonPropertyName("fallbackMessage")]
    public string FallbackMessage { get; set; } = DefaultFallbackMessage;

    /// <summary>reset 生效后回给玩家的确认文案；留空则不发送确认（仅清空历史）。</summary>
    [JsonPropertyName("resetConfirmationMessage")]
    public string ResetConfirmationMessage { get; set; } = DefaultResetConfirmationMessage;

    /// <summary>中文 system prompt 模板路径（相对 mod 根目录；可替换）。</summary>
    [JsonPropertyName("systemPromptPath")]
    public string SystemPromptPath { get; set; } = "config/prompts/system-zh.txt";

    /// <summary>per-trader 配置列表。</summary>
    [JsonPropertyName("traders")]
    public List<TraderConfig> Traders { get; set; } = [];
}

/// <summary>LLM 端点与调用参数（OpenAI 兼容）。</summary>
/// <remarks>
/// 代码兜底默认（下方各属性初值）为**本地 Ollama**（隐私优先、不出网），仅在配置**缺 `llm` 段**时生效。
/// 随包 `config.jsonc` / `defaultConfig.jsonc` 恒存在且显式给出**云端 DeepSeek**（2026-10-14 经 Overseer
/// 决策的发布默认），故实机默认即云端。两处默认的分叉是刻意的：代码不硬编码云端点，避免「未显式配置
/// 却出网」；发布默认由配置层宣告。隐私默认因此改为「以配置显式声明」。
/// </remarks>
public sealed class LlmConfig
{
    /// <summary>OpenAI 兼容 base 端点（不含 /chat/completions）。默认本地 Ollama，不出网。</summary>
    [JsonPropertyName("endpoint")]
    public string Endpoint { get; set; } = "http://127.0.0.1:11434/v1";

    /// <summary>API key（仅服务端持有；本地 Ollama 留空）。日志与异常绝不输出此值。</summary>
    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = "";

    /// <summary>模型名。</summary>
    [JsonPropertyName("model")]
    public string Model { get; set; } = "llama3.1";

    /// <summary>采样温度。</summary>
    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0.7;

    /// <summary>单次回复最大 token 数。</summary>
    [JsonPropertyName("maxTokens")]
    public int MaxTokens { get; set; } = 512;

    /// <summary>单次请求超时（秒）。</summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>回复延迟范围（秒）。</summary>
public sealed class ReplyDelayConfig
{
    /// <summary>延迟下界（秒）。</summary>
    [JsonPropertyName("minSeconds")]
    public double MinSeconds { get; set; } = 2;

    /// <summary>延迟上界（秒）。</summary>
    [JsonPropertyName("maxSeconds")]
    public double MaxSeconds { get; set; } = 6;
}

/// <summary>单个商人的聊天的配置。</summary>
public sealed class TraderConfig
{
    /// <summary>商人 id（MongoId 字符串）；必填，缺失即禁用该商人。</summary>
    [JsonPropertyName("traderId")]
    public string TraderId { get; set; } = "";

    /// <summary>显示名（用于对话挂载与日志）；留空则回退为 traderId。</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    /// <summary>是否启用本商人的聊天。</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>人格提示（参与 system prompt 组装）。</summary>
    [JsonPropertyName("persona")]
    public string Persona { get; set; } = "";

    /// <summary>知识文档目录（相对 mod 根目录）。</summary>
    [JsonPropertyName("docsDir")]
    public string DocsDir { get; set; } = "";

    /// <summary>一次性开场消息；空串表示不发送。</summary>
    [JsonPropertyName("openingMessage")]
    public string OpeningMessage { get; set; } = "";

    /// <summary>清空该商人对话历史的关键词。</summary>
    [JsonPropertyName("resetKeywords")]
    public List<string> ResetKeywords { get; set; } = ["reset", "重置"];

    /// <summary>各态度档位的自定义提示词覆写（可选；键为 hostile/cold/familiar/trusted/ally，大小写不敏感；未配置档位回退内置通用文案）。</summary>
    [JsonPropertyName("attitudePrompts")]
    public Dictionary<string, string> AttitudePrompts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

/// <summary>态度档位规范键（与 AttitudeTier 一一对应；匹配大小写不敏感）。</summary>
public static class TraderAttitudeKeys
{
    /// <summary>敌意/戒备。</summary>
    public const string Hostile = "hostile";

    /// <summary>冷淡/生疏。</summary>
    public const string Cold = "cold";

    /// <summary>熟稔/认可。</summary>
    public const string Familiar = "familiar";

    /// <summary>信任/器重。</summary>
    public const string Trusted = "trusted";

    /// <summary>核心盟友/过命交情。</summary>
    public const string Ally = "ally";

    /// <summary>全部规范键（按档位递进顺序）。</summary>
    public static readonly IReadOnlyList<string> All = [Hostile, Cold, Familiar, Trusted, Ally];
}

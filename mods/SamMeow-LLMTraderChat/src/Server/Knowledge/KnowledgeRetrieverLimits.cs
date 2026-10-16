namespace SamMeow.LLMTraderChat.Server.Knowledge;

/// <summary>
/// 关键词检索的上限（超大目录的明确边界行为：按上限截断，不崩溃）。
/// </summary>
public sealed record KnowledgeRetrieverLimits
{
    /// <summary>单个文档目录最多读取的文件数。</summary>
    public int MaxFiles { get; init; } = 200;

    /// <summary>单文件读取上限（字节）；超过则跳过该文件。</summary>
    public long MaxFileBytes { get; init; } = 1_048_576;

    /// <summary>单个文档目录最多缓存的片段数。</summary>
    public int MaxChunks { get; init; } = 5_000;

    /// <summary>默认上限。</summary>
    public static KnowledgeRetrieverLimits Default { get; } = new();
}

namespace SamMeow.LLMTraderChat.Server.Knowledge;

/// <summary>
/// 知识检索抽象——「检索后端适配口」。聊天引擎只依赖本接口请求「与玩家问题相关的知识片段」；
/// v1 实现为关键词/子串检索（<see cref="KeywordKnowledgeRetriever"/>），
/// 后续可替换为嵌入向量等后端而不改接口形状。
/// </summary>
public interface IKnowledgeRetriever
{
    /// <summary>
    /// 检索与 <paramref name="query"/> 相关的片段，按相关度降序返回至多 <paramref name="topK"/> 条。
    /// 空库、无命中或 <paramref name="topK"/> &lt;= 0 时返回空列表（不抛异常）。
    /// </summary>
    Task<IReadOnlyList<KnowledgeSnippet>> RetrieveAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default);
}

namespace SamMeow.LLMTraderChat.Server.Knowledge;

/// <summary>
/// 检索器工厂接缝：按 per-trader 的文档目录（相对 mod 根目录）产出
/// <see cref="IKnowledgeRetriever"/>。引擎经此接口取检索器，从而只依赖抽象、
/// 并可在测试中注入空实现。
/// </summary>
public interface IKnowledgeRetrieverFactory
{
    /// <summary>为给定文档目录（相对 mod 根目录；空则视为无文档）创建检索器。</summary>
    IKnowledgeRetriever Create(string docsDirectory);
}

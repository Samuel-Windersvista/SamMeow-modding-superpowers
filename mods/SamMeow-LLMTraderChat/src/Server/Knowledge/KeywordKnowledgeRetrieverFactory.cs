using System.Collections.Concurrent;

namespace SamMeow.LLMTraderChat.Server.Knowledge;

/// <summary>
/// 默认检索器工厂：按 per-trader 的 <c>docsDir</c>（相对 mod 根目录）产出关键词检索器，
/// 并按目录缓存实例（复用其内部分块缓存，避免每条消息重读文件）。空 docsDir → 返回空检索器。
/// </summary>
public sealed class KeywordKnowledgeRetrieverFactory : IKnowledgeRetrieverFactory
{
    private readonly string _modRootDirectory;
    private readonly KnowledgeRetrieverLimits _limits;
    private readonly ConcurrentDictionary<string, KeywordKnowledgeRetriever> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>以 mod 根目录构造；<paramref name="limits"/> 缺省用默认上限。</summary>
    public KeywordKnowledgeRetrieverFactory(string modRootDirectory, KnowledgeRetrieverLimits? limits = null)
    {
        _modRootDirectory = modRootDirectory ?? throw new ArgumentNullException(nameof(modRootDirectory));
        _limits = limits ?? KnowledgeRetrieverLimits.Default;
    }

    /// <inheritdoc />
    public IKnowledgeRetriever Create(string docsDirectory)
    {
        var key = docsDirectory ?? string.Empty;
        return _cache.GetOrAdd(key, static (dir, state) =>
        {
            // 空目录 → 传空字符串：KeywordKnowledgeRetriever 对空/不存在目录返回空结果。
            var resolved = string.IsNullOrWhiteSpace(dir) ? string.Empty : Path.Combine(state.Root, dir);
            return new KeywordKnowledgeRetriever(resolved, state.Limits);
        }, (Root: _modRootDirectory, Limits: _limits));
    }
}

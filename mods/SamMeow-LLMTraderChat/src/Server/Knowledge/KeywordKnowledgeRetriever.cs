using System.Text.RegularExpressions;

namespace SamMeow.LLMTraderChat.Server.Knowledge;

/// <summary>
/// 关键词/子串检索实现（v1）。读 per-trader 文档目录（md/txt），按标题/段落分块，
/// 用关键词命中数与权重打分，返回 top-k 片段（带来源文件与标题）。
///
/// 中文兜底：CJK 无空格分词，改为对 CJK 连续段生成字符级双字（bigram），并保留整段作为短语权重。
/// 空库 / 无命中 / topK&lt;=0 返回空列表；超大目录按 <see cref="KnowledgeRetrieverLimits"/> 截断，
/// 不抛异常。首次检索时加载并缓存分块（同实例后续调用复用）。
/// </summary>
public sealed class KeywordKnowledgeRetriever : IKnowledgeRetriever
{
    private static readonly string[] SupportedExtensions = [".md", ".txt"];
    private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.Compiled);
    private static readonly Regex CjkRegex = new(@"[\u4e00-\u9fff]+", RegexOptions.Compiled);

    private const int TitleScoreMultiplier = 2;

    private readonly string _docsDirectory;
    private readonly KnowledgeRetrieverLimits _limits;
    private IReadOnlyList<KnowledgeSnippet>? _chunks;

    /// <summary>以文档目录构造检索器；<paramref name="limits"/> 缺省用默认上限。</summary>
    public KeywordKnowledgeRetriever(string docsDirectory, KnowledgeRetrieverLimits? limits = null)
    {
        _docsDirectory = docsDirectory ?? throw new ArgumentNullException(nameof(docsDirectory));
        _limits = limits ?? KnowledgeRetrieverLimits.Default;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<KnowledgeSnippet>> RetrieveAsync(
        string query, int topK, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (topK <= 0 || string.IsNullOrWhiteSpace(query))
        {
            return Task.FromResult<IReadOnlyList<KnowledgeSnippet>>([]);
        }

        var terms = Tokenize(query);
        if (terms.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<KnowledgeSnippet>>([]);
        }

        var chunks = _chunks ??= LoadChunks();
        var matches = new List<KnowledgeSnippet>();
        foreach (var chunk in chunks)
        {
            var score = Score(chunk, terms);
            if (score > 0)
            {
                matches.Add(chunk with { Score = score });
            }
        }

        var top = matches
            .OrderByDescending(snippet => snippet.Score)
            .ThenBy(snippet => snippet.Source, StringComparer.Ordinal)
            .ThenBy(snippet => snippet.Title, StringComparer.Ordinal)
            .Take(topK)
            .ToList();

        return Task.FromResult<IReadOnlyList<KnowledgeSnippet>>(top);
    }

    /// <summary>加载文档目录（含子目录）内 md/txt 文件并分块，按上限截断。</summary>
    private IReadOnlyList<KnowledgeSnippet> LoadChunks()
    {
        if (string.IsNullOrWhiteSpace(_docsDirectory) || !Directory.Exists(_docsDirectory))
        {
            return [];
        }

        var files = Directory
            .EnumerateFiles(_docsDirectory, "*.*", SearchOption.AllDirectories)
            .Where(path => SupportedExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .Take(_limits.MaxFiles);

        var chunks = new List<KnowledgeSnippet>();
        foreach (var path in files)
        {
            var info = new FileInfo(path);
            if (info.Length == 0 || info.Length > _limits.MaxFileBytes)
            {
                continue;
            }

            string content;
            try
            {
                content = File.ReadAllText(path);
            }
            catch (IOException)
            {
                continue;
            }

            var source = Path.GetRelativePath(_docsDirectory, path).Replace('\\', '/');
            foreach (var chunk in ChunkFile(source, content))
            {
                chunks.Add(chunk);
                if (chunks.Count >= _limits.MaxChunks)
                {
                    return chunks;
                }
            }
        }

        return chunks;
    }

    /// <summary>按 Markdown 标题与空行分段：标题行起新题目，空行冲刷当前段落。</summary>
    private static IEnumerable<KnowledgeSnippet> ChunkFile(string source, string content)
    {
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        string? title = null;
        var buffer = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();
            var trimmedStart = line.TrimStart();

            if (trimmedStart.StartsWith('#'))
            {
                if (buffer.Count > 0)
                {
                    yield return Build(source, title, buffer);
                    buffer.Clear();
                }

                var heading = trimmedStart.TrimStart('#').Trim();
                title = heading.Length == 0 ? null : heading;
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                if (buffer.Count > 0)
                {
                    yield return Build(source, title, buffer);
                    buffer.Clear();
                }

                continue;
            }

            buffer.Add(line);
        }

        if (buffer.Count > 0)
        {
            yield return Build(source, title, buffer);
        }
    }

    private static KnowledgeSnippet Build(string source, string? title, List<string> buffer) =>
        new(source, title ?? source, string.Join('\n', buffer), 0);

    /// <summary>
    /// 切词：非 CJK 词长度 &gt;= 2 才保留（降噪）；CJK 段生成双字 + 整段（短语加权）。
    /// </summary>
    private static List<string> Tokenize(string query)
    {
        var terms = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddTerm(string term)
        {
            var trimmed = term.Trim();
            if (trimmed.Length > 0 && seen.Add(trimmed))
            {
                terms.Add(trimmed);
            }
        }

        foreach (Match word in WordRegex.Matches(query))
        {
            var value = word.Value;
            var cjkRuns = CjkRegex.Matches(value);
            if (cjkRuns.Count == 0)
            {
                if (value.Length >= 2)
                {
                    AddTerm(value);
                }

                continue;
            }

            foreach (var latin in CjkRegex.Replace(value, " ").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (latin.Length >= 2)
                {
                    AddTerm(latin);
                }
            }

            foreach (Match run in cjkRuns)
            {
                var text = run.Value;
                if (text.Length == 1)
                {
                    AddTerm(text);
                    continue;
                }

                AddTerm(text);
                for (var index = 0; index + 1 < text.Length; index++)
                {
                    AddTerm(text.Substring(index, 2));
                }
            }
        }

        return terms;
    }

    /// <summary>片段得分：正文命中权重 + 标题命中权重（×2）。</summary>
    private static int Score(KnowledgeSnippet chunk, List<string> terms) =>
        CountWeight(chunk.Text, terms) + (CountWeight(chunk.Title, terms) * TitleScoreMultiplier);

    /// <summary>统计每个词在文本中的出现次数 × 词长（长词更具体，权重更高）。</summary>
    private static int CountWeight(string haystack, List<string> terms)
    {
        var score = 0;
        foreach (var term in terms)
        {
            var index = 0;
            while ((index = haystack.IndexOf(term, index, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                score += term.Length;
                index += term.Length;
            }
        }

        return score;
    }
}

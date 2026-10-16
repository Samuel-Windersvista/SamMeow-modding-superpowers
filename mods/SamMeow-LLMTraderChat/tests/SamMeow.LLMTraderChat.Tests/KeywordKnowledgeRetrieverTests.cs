using System.Text;
using SamMeow.LLMTraderChat.Server.Knowledge;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>
/// 检索层单测（工单 05）：命中、排序、top-k、空库、中文匹配。
/// 用临时 fixture 文档目录，不依赖网络与游戏安装。
/// </summary>
public class KeywordKnowledgeRetrieverTests
{
    [Fact]
    public async Task Retrieve_Hit_ReturnsSnippetWithSourceAndTitle()
    {
        var directory = CreateDocs(("a.md", "# Alpha\nunique-token here"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("unique-token", topK: 5);

            var snippet = Assert.Single(results);
            Assert.Equal("a.md", snippet.Source);
            Assert.Equal("Alpha", snippet.Title);
            Assert.Contains("unique-token", snippet.Text, StringComparison.Ordinal);
            Assert.True(snippet.Score > 0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_RanksHigherOccurrenceFirst()
    {
        var directory = CreateDocs(
            ("b.md", "# Beta\nalpha"),
            ("a.md", "# Alpha\nalpha alpha alpha"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("alpha", topK: 5);

            Assert.Equal(2, results.Count);
            Assert.Equal("a.md", results[0].Source);
            Assert.Equal("b.md", results[1].Source);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_RespectsTopK()
    {
        var directory = CreateDocs(
            ("a.md", "# A\nneedle"),
            ("b.md", "# B\nneedle"),
            ("c.md", "# C\nneedle"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("needle", topK: 2);

            Assert.Equal(2, results.Count);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_EmptyDirectory_ReturnsEmpty()
    {
        var directory = CreateDocs();
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("anything", topK: 5);

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_MissingDirectory_ReturnsEmpty()
    {
        var missing = Path.Combine(Path.GetTempPath(), "llmtc-kb-missing-" + Guid.NewGuid().ToString("N"));
        var retriever = new KeywordKnowledgeRetriever(missing);

        var results = await retriever.RetrieveAsync("anything", topK: 5);

        Assert.Empty(results);
    }

    [Fact]
    public async Task Retrieve_ChineseBigram_Matches()
    {
        var directory = CreateDocs(("faq.md", "# 保险\n保险理赔流程说明：理赔需提供证据。"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("保险理赔", topK: 5);

            var snippet = Assert.Single(results);
            Assert.Equal("faq.md", snippet.Source);
            Assert.True(snippet.Score > 0);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_NoMatch_ReturnsEmpty()
    {
        var directory = CreateDocs(("a.md", "# Alpha\nhello world"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("zzzzz", topK: 5);

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_IgnoresUnsupportedExtensions()
    {
        var directory = CreateDocs(("a.json", "{\"content\":\"needle\"}"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("needle", topK: 5);

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_NonPositiveTopK_ReturnsEmpty()
    {
        var directory = CreateDocs(("a.md", "# Alpha\nneedle"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory);

            var results = await retriever.RetrieveAsync("needle", topK: 0);

            Assert.Empty(results);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Retrieve_RespectsMaxFilesLimit()
    {
        var directory = CreateDocs(
            ("a.md", "# A\nneedle"),
            ("b.md", "# B\nneedle"));
        try
        {
            var retriever = new KeywordKnowledgeRetriever(directory, new KnowledgeRetrieverLimits { MaxFiles = 1 });

            var results = await retriever.RetrieveAsync("needle", topK: 5);

            // 目录按路径排序，MaxFiles=1 只读 a.md；b.md 被上限截断。
            var snippet = Assert.Single(results);
            Assert.Equal("a.md", snippet.Source);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>建一个临时文档目录并写入 fixture 文件。</summary>
    private static string CreateDocs(params (string Name, string Content)[] files)
    {
        var directory = Path.Combine(Path.GetTempPath(), "llmtc-kb-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        foreach (var (name, content) in files)
        {
            var path = Path.Combine(directory, name);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content, new UTF8Encoding(false));
        }

        return directory;
    }
}

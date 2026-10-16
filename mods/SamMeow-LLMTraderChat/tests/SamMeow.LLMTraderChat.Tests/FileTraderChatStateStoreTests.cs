using SamMeow.LLMTraderChat.Server.Spt;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>开场一次性状态存储的持久化单测（工单 07）。不依赖 SPT 运行时。</summary>
public class FileTraderChatStateStoreTests
{
    [Fact]
    public async Task Mark_Then_IsSent_True_AndKeysAreIsolated()
    {
        var directory = CreateTempDirectory();
        try
        {
            var store = new FileTraderChatStateStore(directory);

            Assert.False(await store.IsOpeningSentAsync("s1", "t1"));

            await store.MarkOpeningSentAsync("s1", "t1");

            Assert.True(await store.IsOpeningSentAsync("s1", "t1"));
            Assert.False(await store.IsOpeningSentAsync("s2", "t1"));
            Assert.False(await store.IsOpeningSentAsync("s1", "t2"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TryClaim_FirstTrue_SecondFalse()
    {
        var directory = CreateTempDirectory();
        try
        {
            var store = new FileTraderChatStateStore(directory);

            Assert.True(await store.TryClaimOpeningAsync("s1", "t1"));
            Assert.False(await store.TryClaimOpeningAsync("s1", "t1"));
            Assert.True(await store.IsOpeningSentAsync("s1", "t1"));
            // 不同键互不影响
            Assert.True(await store.TryClaimOpeningAsync("s2", "t1"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TryClaim_Concurrent_OnlyOneSucceeds()
    {
        var directory = CreateTempDirectory();
        try
        {
            var store = new FileTraderChatStateStore(directory);

            var results = await Task.WhenAll(
                Enumerable.Range(0, 16).Select(_ => store.TryClaimOpeningAsync("s1", "t1")));

            Assert.Single(results, claimed => claimed);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Mark_PersistsAcrossInstances()
    {
        var directory = CreateTempDirectory();
        try
        {
            await new FileTraderChatStateStore(directory).MarkOpeningSentAsync("s1", "t1");

            var reopened = new FileTraderChatStateStore(directory);

            Assert.True(await reopened.IsOpeningSentAsync("s1", "t1"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "llmtc-state-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}

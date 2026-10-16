using SamMeow.LLMTraderChat.Shared;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>客户端商人名单解析/生成的纯逻辑单测（工单 07 第 6 项）。</summary>
public class ClientTraderListTests
{
    private static readonly IReadOnlyList<string> Defaults = new[] { "a", "b" };

    [Fact]
    public void Parse_ValidJson_ReturnsIdsInOrder()
    {
        var ids = ClientTraderList.Parse("""{ "enabledTraderIds": ["54cb", "5935"] }""", Defaults);

        Assert.Equal(new[] { "54cb", "5935" }, ids);
    }

    [Fact]
    public void Parse_MissingKey_ReturnsDefaults()
    {
        Assert.Equal(Defaults, ClientTraderList.Parse("{}", Defaults));
    }

    [Fact]
    public void Parse_Malformed_ReturnsDefaults()
    {
        Assert.Equal(Defaults, ClientTraderList.Parse("not json at all", Defaults));
        Assert.Equal(Defaults, ClientTraderList.Parse("""{ "enabledTraderIds": [ """, Defaults));
    }

    [Fact]
    public void Parse_NullOrEmpty_ReturnsDefaults()
    {
        Assert.Equal(Defaults, ClientTraderList.Parse(null, Defaults));
        Assert.Equal(Defaults, ClientTraderList.Parse("", Defaults));
    }

    [Fact]
    public void Parse_ExplicitEmptyArray_ReturnsEmptyList_NotDefaults()
    {
        // 显式 [] 语义 = 不解锁任何商人（不回退默认）。
        Assert.Empty(ClientTraderList.Parse("""{ "enabledTraderIds": [] }""", Defaults));
        Assert.Empty(ClientTraderList.Parse("""{ "enabledTraderIds": [ ] }""", Defaults));
    }

    [Fact]
    public void Parse_DeduplicatesIds()
    {
        var ids = ClientTraderList.Parse("""{ "enabledTraderIds": ["x", "x", "y"] }""", Defaults);

        Assert.Equal(new[] { "x", "y" }, ids);
    }

    [Fact]
    public void Contains_IsCaseInsensitive()
    {
        var ids = new[] { "54CB57776803FA99248B456E" };

        Assert.True(ClientTraderList.Contains(ids, "54cb57776803fa99248b456e"));
        Assert.False(ClientTraderList.Contains(ids, "other"));
        Assert.False(ClientTraderList.Contains(Array.Empty<string>(), "anything"));
    }

    [Fact]
    public void BuildDefault_RoundTripsThroughParse()
    {
        var text = ClientTraderList.BuildDefault(Defaults);

        Assert.Equal(Defaults, ClientTraderList.Parse(text, Array.Empty<string>()));
        Assert.Contains("enabledTraderIds", text, StringComparison.Ordinal);
    }
}

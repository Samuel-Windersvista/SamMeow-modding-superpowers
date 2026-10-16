using System.Reflection;
using SamMeow.LLMTraderChat.Server;
using SamMeow.LLMTraderChat.Shared;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Xunit;
using Version = SemanticVersioning.Version;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>
/// 脚手架冒烟测试：断言服务端元数据与共享常量符合 spec 约定。
/// 不触碰 SPT 运行时、不联网、不依赖游戏安装。
/// </summary>
public class ScaffoldSmokeTests
{
    private static readonly Assembly ServerAssembly = typeof(ModMetadata).Assembly;
    private static readonly Assembly SharedAssembly = typeof(SharedConstants).Assembly;

    [Fact]
    public void ServerAssembly_ContainsExactlyOne_IModMetadataImplementation()
    {
        var implementations = ServerAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IModMetadata).IsAssignableFrom(type))
            .ToList();

        Assert.Single(implementations);
    }

    [Fact]
    public void SharedAssembly_ContainsNo_IModMetadataImplementation()
    {
        // Shared.dll 随服务端部署在同一 user/mods/<Mod>/ 目录，必须不实现 IModMetadata
        // （否则 SPT 的 SingleOrDefault 会因「重复元数据」拒载）。
        var implementations = SharedAssembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IModMetadata).IsAssignableFrom(type))
            .ToList();

        Assert.Empty(implementations);
    }

    [Fact]
    public void Metadata_ExposesExpectedIdentityFields()
    {
        var metadata = new ModMetadata();

        Assert.Equal("sammeow.llmtraderchat", metadata.ModGuid);
        Assert.Equal("LLM Trader Chat", metadata.Name);
        Assert.Equal("SamMeow", metadata.Author);
        // 版本字面量：改 Directory.Build.props 的 <Version> 时同步更新此处。
        Assert.Equal("0.1.0", metadata.Version.ToString());
        Assert.Equal("MIT", metadata.License);
        Assert.False(metadata.HasPrepatcher);
    }

    [Fact]
    public void Metadata_SptVersionRange_CoversFourOneSix()
    {
        var metadata = new ModMetadata();

        // 目标环境 SPT 4.1.6：SptVersion 必须覆盖 4.1.6，否则 ModValidator 拒载。
        Assert.True(metadata.SptVersion.IsSatisfied(new Version("4.1.6")));
    }

    [Fact]
    public void SharedConstants_MatchSpec()
    {
        Assert.Equal("sammeow.llmtraderchat", SharedConstants.ModGuid);
        Assert.Equal("LLM Trader Chat", SharedConstants.ModName);
        Assert.Equal(1, SharedConstants.ProtocolVersion);
    }
}

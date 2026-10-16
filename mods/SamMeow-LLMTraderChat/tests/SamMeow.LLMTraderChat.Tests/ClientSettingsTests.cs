using SamMeow.LLMTraderChat.Shared;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>客户端设置加载与解锁判定的单测（工单 07 NIT-6）。纯 Shared 逻辑，不依赖 BepInEx。</summary>
public class ClientSettingsTests
{
    [Fact]
    public void Load_MissingFile_WritesDefaultAndUsesDefaults()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "traders.json");

            var settings = ClientSettings.Load(true, path);

            Assert.True(File.Exists(path));
            Assert.Equal(SharedConstants.DefaultEnabledTraderIds, settings.EnabledTraderIds);
            Assert.True(settings.ShouldUnlock(SharedConstants.TherapistTraderId));
            Assert.True(settings.ShouldUnlock(SharedConstants.PeacekeeperTraderId));
            // 工单 08：发布的客户端默认名单须包含 mod 商人 Painter（零代码接入的默认解锁值）。
            Assert.True(settings.ShouldUnlock(SharedConstants.PainterTraderId));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_ExplicitEmptyArray_UnlocksNothing()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "traders.json");
            File.WriteAllText(path, """{ "enabledTraderIds": [] }""");

            var settings = ClientSettings.Load(true, path);

            Assert.Empty(settings.EnabledTraderIds);
            Assert.False(settings.ShouldUnlock(SharedConstants.TherapistTraderId));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_MasterSwitchOff_UnlocksNothing()
    {
        var directory = CreateTempDirectory();
        try
        {
            var settings = ClientSettings.Load(false, Path.Combine(directory, "traders.json"));

            Assert.False(settings.ShouldUnlock(SharedConstants.TherapistTraderId));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ShouldUnlock_TraderIdIsCaseInsensitive()
    {
        var directory = CreateTempDirectory();
        try
        {
            var path = Path.Combine(directory, "traders.json");
            File.WriteAllText(path, """{ "enabledTraderIds": ["54CB57776803FA99248B456E"] }""");

            var settings = ClientSettings.Load(true, path);

            Assert.True(settings.ShouldUnlock("54cb57776803fa99248b456e"));
            Assert.False(settings.ShouldUnlock("no-such-trader"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), "llmtc-clientsettings-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}

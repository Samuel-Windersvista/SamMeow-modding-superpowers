using SamMeow.LLMTraderChat.Shared;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>聊天输入字符校验放行策略的单测（工单 12）。</summary>
public class ChatInputPolicyTests
{
    [Fact]
    public void ShouldBypass_UnlockedTrader_ReturnsTrue()
    {
        var settings = ClientSettings.Default;

        Assert.True(ChatInputPolicy.ShouldBypassCharacterValidation(settings, SharedConstants.TherapistTraderId));
        Assert.True(ChatInputPolicy.ShouldBypassCharacterValidation(settings, SharedConstants.PeacekeeperTraderId));
    }

    [Fact]
    public void ShouldBypass_UnknownDialogue_ReturnsFalse()
    {
        // 名单外的对话（好友 / 原版 bot 等）保持原版校验。
        Assert.False(ChatInputPolicy.ShouldBypassCharacterValidation(ClientSettings.Default, "some-friend-or-bot-id"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void ShouldBypass_MissingDialogueId_ReturnsFalse(string? dialogueId)
    {
        Assert.False(ChatInputPolicy.ShouldBypassCharacterValidation(ClientSettings.Default, dialogueId));
    }

    [Fact]
    public void ShouldBypass_NullSettings_ReturnsFalse()
    {
        Assert.False(ChatInputPolicy.ShouldBypassCharacterValidation(null!, SharedConstants.TherapistTraderId));
    }

    [Fact]
    public void ShouldBypass_MasterSwitchOff_ReturnsFalse()
    {
        var directory = Path.Combine(Path.GetTempPath(), "llmtc-chatpolicy-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var settings = ClientSettings.Load(false, Path.Combine(directory, "traders.json"));

            Assert.False(ChatInputPolicy.ShouldBypassCharacterValidation(settings, SharedConstants.TherapistTraderId));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}

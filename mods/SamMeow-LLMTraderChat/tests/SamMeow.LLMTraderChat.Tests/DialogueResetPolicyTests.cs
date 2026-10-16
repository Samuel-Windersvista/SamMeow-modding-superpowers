using SamMeow.LLMTraderChat.Server.Spt;
using Xunit;

namespace SamMeow.LLMTraderChat.Tests;

/// <summary>reset 选择性清空规则的单测（工单 07 SHOULD-1）。bot 依赖 SPT 运行时不可直测，故测此纯逻辑。</summary>
public class DialogueResetPolicyTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(5, false)]
    public void ShouldRemoveOnReset_KeepsMessagesWithAttachments(int attachmentItemCount, bool expected)
    {
        Assert.Equal(expected, DialogueResetPolicy.ShouldRemoveOnReset(attachmentItemCount));
    }
}

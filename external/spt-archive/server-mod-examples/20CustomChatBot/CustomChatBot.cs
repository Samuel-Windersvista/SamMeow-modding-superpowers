using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialogue;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;

namespace _20CustomChatBot;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.customchatbot";
    public override string Name { get; init; } = "CustomChatBotExample";
    public override string Author { get; init; } = "SPTarkov";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}

[Injectable]
public class CustomChatBot(
    MailSendService mailSendService) : IDialogueChatBot
{
    public UserDialogInfo GetChatBot()
    {
        return new UserDialogInfo
        {
            Id = "modderBuddy",
            Aid = 9999999,
            Info = new UserDialogDetails
            {
                Nickname = "Buddy",
                Side = "Usec",
                Level = 69,
                MemberCategory = MemberCategory.Sherpa,
                SelectedMemberCategory = MemberCategory.Sherpa
            }
        };
    }

    public ValueTask<string> HandleMessage(MongoId sessionId, SendMessageRequest request)
    {
        mailSendService.SendUserMessageToPlayer(
            sessionId,
            GetChatBot(),
            $"Im Buddy! I just reply back what you typed to me!\n{request.Text}");

        return ValueTask.FromResult(request.DialogId);
    }
}

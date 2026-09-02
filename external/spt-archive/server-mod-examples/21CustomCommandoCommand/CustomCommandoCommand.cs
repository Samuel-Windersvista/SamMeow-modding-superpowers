using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Dialog.Commando;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace _21CustomCommandoCommand;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.customcommandocommand";
    public override string Name { get; init; } = "CustomCommandoCommandExample";
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
public class CustomCommandoCommand(
    DatabaseServer databaseServer,
    MailSendService mailSendService) : IChatCommand
{
    public string GetCommandHelp(string command)
    {
        if (command == "talk")
        {
            return "Usage: test talk";
        }

        return null;
    }

    public ValueTask<string> Handle(string command, UserDialogInfo commandHandler, MongoId sessionId, SendMessageRequest request)
    {
        if (command == "talk")
        {
            mailSendService.SendUserMessageToPlayer(sessionId, commandHandler, $"IM TALKING! OKAY?!\nHere's the walk speed X config from the DB: {databaseServer.GetTables().Globals.Configuration.WalkSpeed.X}");
            return new ValueTask<string>(request.DialogId);
        }

        return new ValueTask<string>(string.Empty);
    }

    public string CommandPrefix { get; }
    public List<string> Commands => ["talk"];
}

using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers.Ws;
using System.Net.WebSockets;
using System.Text;

namespace _24Websocket;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.websocket";
    public override string Name { get; init; } = "CustomWebSocketConnectionHandlerExample";
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

[Injectable(InjectionType = InjectionType.Singleton)]
public class CustomWebSocketConnectionHandler(
    ISptLogger<CustomWebSocketConnectionHandler> logger) : IWebSocketConnectionHandler
{
    public string GetHookUrl()
    {
        return "/custom/socket/";
    }

    public string GetSocketId()
    {
        return "My Custom WebSocket";
    }

    public Task OnConnection(WebSocket ws, HttpContext context, string sessionIdContext)
    {
        logger.Info("Custom web socket is now connected!");
        
        return Task.CompletedTask;
    }

    public async Task OnMessage(byte[] rawData, WebSocketMessageType messageType, WebSocket ws, HttpContext context)
    {
        var msg = Encoding.UTF8.GetString(rawData);

        if (msg == "toodaloo")
        {
            await ws.SendAsync(Encoding.UTF8.GetBytes("toodaloo back!"), WebSocketMessageType.Text, true, CancellationToken.None);
        }
    }

    public Task OnClose(WebSocket ws, HttpContext context, string sessionIdContext)
    {
        return Task.CompletedTask;
    }
}

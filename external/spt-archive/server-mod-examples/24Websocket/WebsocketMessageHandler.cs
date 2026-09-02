using System.Net.WebSockets;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers.Ws.Message;

namespace _24Websocket;

public class WebsocketMessageHandler(
    ISptLogger<WebsocketMessageHandler> logger) : ISptWebSocketMessageHandler
{
    public Task OnSptMessage(string sessionID, WebSocket client, byte[] rawData)
    {
        logger.Info($"Custom SPT WebSocket Message handler received a message for {sessionID}: {rawData.ToString()}");
        return Task.CompletedTask;
    }
}

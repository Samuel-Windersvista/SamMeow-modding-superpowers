using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using PlaceholderServer.Config;

namespace PlaceholderServer;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class PlaceholderServerEntry(
    ISptLogger<PlaceholderServerEntry> logger,
    PlaceholderServerConfig config) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info($"placeholder-server fixture loaded, enabled={config.Enabled}");
        return Task.CompletedTask;
    }
}

public class PlaceholderServerConfigRegistration : IOnDIConstruct
{
    public void OnDIConstruct(IServiceCollection services)
    {
        services.AddSingleton<PlaceholderServerConfig>();
    }
}

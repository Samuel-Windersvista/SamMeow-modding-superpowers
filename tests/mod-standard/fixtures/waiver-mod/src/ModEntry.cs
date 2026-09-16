using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using WaiverMod.Config;

namespace WaiverMod;

[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class WaiverModEntry(
    ISptLogger<WaiverModEntry> logger,
    WaiverModConfig config) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info($"waiver-mod fixture loaded, enabled={config.Enabled}");
        return Task.CompletedTask;
    }
}

public class WaiverModConfigRegistration : IOnDIConstruct
{
    public void OnDIConstruct(IServiceCollection services)
    {
        services.AddSingleton<WaiverModConfig>();
    }
}

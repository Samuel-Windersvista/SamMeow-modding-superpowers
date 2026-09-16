using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.DI;
using FailServer.Config;

namespace FailServer;

// 故意不标注 Injectable 特性：触发 STD-SRV-001
public class FailServerEntry : IOnLoad
{
    private readonly ISptLogger<FailServerEntry> _logger;

    public FailServerEntry(ISptLogger<FailServerEntry> logger)
    {
        _logger = logger;
    }

    // 故意写裸数字：触发 STD-SRV-002
    public int TypePriority { get; set; } = 5;

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // 故意用 Console.Write：触发 STD-LOG-001
        Console.Write("fail-server fixture");
        _logger.Info("unreachable");
        return Task.CompletedTask;
    }
}

// IOnDIConstruct 存在，保证 CFG-003 仍 PASS
public class FailServerConfigRegistration : IOnDIConstruct
{
    public void OnDIConstruct(IServiceCollection services)
    {
        services.AddSingleton<FailServerConfig>();
    }
}

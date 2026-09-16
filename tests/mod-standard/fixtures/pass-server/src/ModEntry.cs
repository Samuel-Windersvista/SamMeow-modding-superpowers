using Microsoft.Extensions.DependencyInjection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using PassServer.Config;

namespace PassServer;

// STD-SRV-001：服务端类用 [Injectable] 交由 DI 容器构造
// STD-SRV-002：TypePriority 基于 OnLoadOrder.X 写偏移，禁止裸数字
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class PassServerEntry(
    ISptLogger<PassServerEntry> logger,
    PassServerConfig config) : IOnLoad
{
    // STD-SRV-003：生命周期方法用 async 签名并传播 CancellationToken
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // STD-SRV-008：用注入的 ISptLogger<T> 记录日志
        logger.Info($"pass-server fixture loaded, enabled={config.Enabled}");
        return Task.CompletedTask;
    }
}

// STD-CFG-003：用 IOnDIConstruct 与 AddSingleton 注册配置
public class PassServerConfigRegistration : IOnDIConstruct
{
    public void OnDIConstruct(IServiceCollection services)
    {
        services.AddSingleton<PassServerConfig>();
    }
}

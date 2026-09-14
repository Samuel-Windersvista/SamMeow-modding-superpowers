using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using {{ROOT_NAMESPACE}}.Config;
using {{ROOT_NAMESPACE}}.Services;

namespace {{ROOT_NAMESPACE}};

/// <summary>
/// Mod 入口：标 [Injectable] 让 DI 容器发现并构造（STD-SRV-001）。
/// STD-SRV-002：TypePriority 决定在服务器加载序列中的位置（常量见 OnLoadOrder），
/// 永远基于 OnLoadOrder.X 写偏移，不要写裸数字。
/// PostLoad + 1 = 在所有内置阶段之后执行，适合"加载完成后再做自己的事"。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class {{MOD_CLASS_NAME}}Entry(
    ISptLogger<{{MOD_CLASS_NAME}}Entry> logger,
    {{MOD_CLASS_NAME}}Config config,
    {{MOD_CLASS_NAME}}Service service) : IOnLoad
{
    /// <summary>
    /// STD-SRV-003：4.1 生命周期方法全部 async 且带 CancellationToken（服务器关机时取消）。
    /// STD-LOG-005：取消不是错误，让 OperationCanceledException 正常传播即可。
    /// </summary>
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // STD-SRV-001 / STD-SRV-008：服务与日志器均由构造函数注入。
        int itemCount = service.GetTemplateItemCount();

        // STD-CFG-003：配置经 IOnDIConstruct + AddSingleton 注册后构造注入。
        if (!config.Enabled)
        {
            logger.Info($"{{MOD_NAME}} 已在 config.jsonc 中禁用，跳过初始化");
            return Task.CompletedTask;
        }

        logger.Success(
            $"{{MOD_NAME}} 已加载，物品模板数量: {itemCount}，exampleMultiplier={config.ExampleMultiplier}");

        return Task.CompletedTask;
    }
}

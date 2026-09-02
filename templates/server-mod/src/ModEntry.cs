using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using {{ROOT_NAMESPACE}}.Services;

namespace {{ROOT_NAMESPACE}};

/// <summary>
/// Mod 入口：标 [Injectable] 让 DI 容器发现并构造。
/// TypePriority 决定在服务器加载序列中的位置（常量见 OnLoadOrder），
/// 永远基于 OnLoadOrder.X 写偏移，不要写裸数字。
/// PostLoad + 1 = 在所有内置阶段之后执行，适合"加载完成后再做自己的事"。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class {{MOD_CLASS_NAME}}Entry(
    ISptLogger<{{MOD_CLASS_NAME}}Entry> logger,
    {{MOD_CLASS_NAME}}Service service) : IOnLoad
{
    /// <summary>
    /// 4.1 生命周期方法全部 async 且带 CancellationToken（服务器关机时取消）。
    /// 取消不是错误：让 OperationCanceledException 正常传播即可。
    /// </summary>
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // 演示：调用注入的服务读一次数据库表
        int itemCount = service.GetTemplateItemCount();
        logger.Success($"{{MOD_NAME}} 已加载，物品模板数量: {itemCount}");

        return Task.CompletedTask;
    }
}

using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace InescapableTarkovsSoftcore;

/// <summary>
/// 服务端加载钩子。在 <see cref="OnLoadOrder.PostLoad"/> + 1 阶段运行一次。
/// 阶段语义（准确理解）：本阶段是全部 mod 装载回调的最末窗口，运行于包内内容 mod
/// （WTT-ContentBackport/Armory/Artem、Epic AIO、TGC-NG、Russian-SF-Pack 等）完成物品注入
/// 之后，故此处进行数据库编辑可看见这些 mod 注入的物品。n=1 保证晚于全部内容注入。
/// 流程：输出固定启动行 → 加载配置（转发告警）→ 运行编排器（汇总由编排器唯一输出）。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public sealed class ModLoadHook(
    ISptLogger<ModLoadHook> logger,
    ConfigLoader configLoader,
    ModuleOrchestrator orchestrator) : IOnLoad
{
    /// <summary>固定启动日志行，由测试断言并在部署阶段 grep 核验。</summary>
    public static readonly string LoadLogLine = $"[ITS] {ModIdentity.Name} v{ModIdentity.Version} loaded";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info(LoadLogLine);

        var load = configLoader.Load();
        foreach (var warning in load.Warnings)
        {
            logger.Warning(warning);
        }

        orchestrator.Run(load.Config);

        return Task.CompletedTask;
    }
}

using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SamMeow.DoorBreachOverhaulServer.Services;

namespace SamMeow.DoorBreachOverhaulServer;

/// <summary>
/// Mod 入口：标 [Injectable] 让 DI 容器发现并构造（STD-SRV-001）。
/// STD-SRV-002：TypePriority 决定在服务器加载序列中的位置（常量见 OnLoadOrder），
/// 永远基于 OnLoadOrder.X 写偏移，不要写裸数字。
///
/// 为什么是 Preload 而不是 PostLoad：
///   4.1.6 实测（反编译 SPTStartupHostedService / SaveCallbacks / ProgramExtensions）——
///   - OnLoadOrder.Preload..(PostLoad-1) 的 IOnLoad 由 ProgramExtensions.RunPreSptLoadCallbacks
///     在数据库加载完成后、Web 服务启动前执行，此时可安全往 TemplateTable / HideoutTable 写入；
///   - OnLoadOrder.SaveCallbacks (600000) 会把 CustomItemService.ProfilesLoaded 置为 true，
///     之后调用 CreateItem 会抛 InvalidOperationException（"items must be added before profiles load"）；
///   - PostLoad (1000000) 晚于 SaveCallbacks，在 PostLoad 注册物品必然失败。
///   故本单采用 OnLoadOrder.Preload + 1（与 4.1 迁移文档一致）。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class DoorBreachOverhaulServerEntry(
    ISptLogger<DoorBreachOverhaulServerEntry> logger,
    C4RegistrationService c4RegistrationService) : IOnLoad
{
    /// <summary>
    /// STD-SRV-003：4.1 生命周期方法全部 async 且带 CancellationToken（服务器关机时取消）。
    /// STD-LOG-005：取消不是错误，让 OperationCanceledException 正常传播即可。
    /// </summary>
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // 注册 C4 物品（模板 + locale + 手册/跳蚤价）与工作台配方；失败就地抛异常（fail-fast）。
        C4RegistrationResult result = c4RegistrationService.Register(cancellationToken);

        // STD-LOG-002：加载成功用 Success 级别；此单条成功行供运行时断言（STD-VERIFY-003）。
        logger.Success(
            $"[DoorBreachOverhaulServer] 已加载：C4 物品 tpl={result.ItemId}，工作台配方 id={result.RecipeId}");

        return Task.CompletedTask;
    }
}

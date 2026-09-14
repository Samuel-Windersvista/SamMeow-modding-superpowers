using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace {{ROOT_NAMESPACE}}.Services;

/// <summary>
/// 示例服务：演示 4.1 的表注入模式。
/// STD-SRV-001：服务端类用 [Injectable] 交由 DI 容器构造；需跨类共享同一实例时显式
///              InjectionType.Singleton（默认是 Transient）。
/// 4.1 移除了 DatabaseServer/DatabaseService，每个数据库表都是 DI 单例，
/// 构造函数直接声明参数即可拿到（表模型位于 SPTarkov.Server.Core.Models.Spt.Tables）。
/// </summary>
// STD-SRV-001
[Injectable(InjectionType.Singleton)]
public class {{MOD_CLASS_NAME}}Service(
    GlobalTable globalTable,
    TemplateTable templateTable,
    ISptLogger<{{MOD_CLASS_NAME}}Service> logger)
{
    /// <summary>读取 TemplateTable.Items（物品模板字典，键为 MongoId）</summary>
    public int GetTemplateItemCount() => templateTable.Items.Count;

    /// <summary>演示修改 GlobalTable（全局配置表）：改 scav 复活冷却（秒）</summary>
    public void SetScavCooldown(int seconds)
    {
        globalTable.Configuration.SavagePlayCooldown = seconds;
        // STD-SRV-008 / STD-LOG-001：日志经构造函数注入的 ISptLogger<T> 输出。
        logger.Info($"已修改 scav 冷却时间为 {seconds} 秒");
    }
}

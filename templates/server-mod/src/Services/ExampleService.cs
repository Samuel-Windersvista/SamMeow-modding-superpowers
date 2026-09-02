using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace {{ROOT_NAMESPACE}}.Services;

/// <summary>
/// 示例服务：演示 4.1 的表注入模式。
/// 4.1 移除了 DatabaseServer/DatabaseService，每个数据库表都是 DI 单例，
/// 构造函数直接声明参数即可拿到（表模型位于 SPTarkov.Server.Core.Models.Spt.Tables）。
/// </summary>
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
        logger.Info($"已修改 scav 冷却时间为 {seconds} 秒");
    }
}

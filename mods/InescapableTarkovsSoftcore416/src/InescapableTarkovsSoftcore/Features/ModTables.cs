using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// SPT 数据库表访问载体：编排器与功能模块经此读取内存表，避免各自注入多张表。
/// 表实例由 SPT 注册为单例（见 KB api-notes-5.0：应注入具体表类型）。
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class ModTables(
    TemplateTable templateTable,
    HideoutTable hideoutTable,
    LocationTable locationTable,
    TradersTable tradersTable,
    GlobalTable globalTable,
    BotTable botTable)
{
    public TemplateTable TemplateTable { get; } = templateTable;

    public HideoutTable HideoutTable { get; } = hideoutTable;

    public LocationTable LocationTable { get; } = locationTable;

    public TradersTable TradersTable { get; } = tradersTable;

    public GlobalTable GlobalTable { get; } = globalTable;

    /// <summary>机器人表（G6-D Reshala 装备改动）。</summary>
    public BotTable BotTable { get; } = botTable;
}

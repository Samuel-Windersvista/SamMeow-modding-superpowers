using System.Text;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;

namespace SptDbDump;

/// <summary>
/// 行为验证工具 mod：启动后 dump 关键数据库表到 JSON 文件。
/// 用于 3.11->4.1 迁移的数据库状态 diff（干净 4.1 vs 带 mod 4.1）。
///
/// 输出：<SPT>/user/mods/SptDbDump/dump/<table>.json
/// 用法：
///   1. 干净 4.1（无业务 mod）启动一次 -> baseline dump
///   2. 带目标 mod 启动一次 -> migrated dump
///   3. diff 两份 dump，应恰好等于目标 mod 的预期改动
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 100)]
public class SptDbDumpEntry(
    ISptLogger<SptDbDumpEntry> logger,
    JsonUtil jsonUtil,
    TemplateTable templateTable,
    TradersTable tradersTable,
    GlobalTable globalTable,
    LocaleTable localeTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info("SptDbDump 开始 dump 关键表...");

        var dumpDir = Path.Combine(
            Directory.GetCurrentDirectory(),
            "user", "mods", "SptDbDump", "dump");

        Directory.CreateDirectory(dumpDir);

        // 触发 locale 懒加载（确保 transformer 生效后再 dump）
        var locales = new Dictionary<string, object>();
        foreach (var (lang, lazy) in localeTable.Global)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                locales[lang] = lazy.Value ?? new SPTarkov.Server.Core.Models.Spt.Tables.GlobalLocaleDictionary();
            }
            catch (Exception ex)
            {
                locales[lang] = $"<load error: {ex.Message}>";
            }
        }

        var tables = new Dictionary<string, object?>
        {
            ["templateItems"] = templateTable.Items,
            ["templateQuests"] = templateTable.Quests,
            ["templateHandbook"] = templateTable.Handbook,
            ["traders"] = tradersTable,
            ["globalsConfig"] = globalTable.Configuration,
            ["locales"] = locales,
        };

        foreach (var (name, data) in tables)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var json = jsonUtil.Serialize(data, true);
            var path = Path.Combine(dumpDir, $"{name}.json");
            File.WriteAllText(path, json, new UTF8Encoding(false));
            logger.Info($"  dumped {name}.json ({(json?.Length ?? 0) / 1024} KB)");
        }

        logger.Success("SptDbDump 完成，dump 目录: " + dumpDir);
        return Task.CompletedTask;
    }
}

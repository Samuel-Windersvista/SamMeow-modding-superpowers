using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace LocaleTest;

/// <summary>
/// locale 阻断器验证 mod：
/// 用官方 LazyLoad.AddTransformer 机制修改 locale（模拟 3.11 的 ETT 类 mod 行为），
/// 验证修改是否在服务器运行时生效。
///
/// 修改目标：给 "Offline raid test mode" 追加标记，模拟 ETT 改任务描述的模式。
/// 验证方式：启动后读回该 key，确认 transformer 生效；随后用 dbdump 对比。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 101)]
public class LocaleTestEntry(
    ISptLogger<LocaleTestEntry> logger,
    LocaleTable localeTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        logger.Info("LocaleTest: 注册 locale transformer...");

        // 官方模式（PostDbLoadService.RenamePreraidLocales 同款）
        if (localeTable.Global.TryGetValue("en", out var enLazy))
        {
            enLazy.AddTransformer(data =>
            {
                if (data is null) return data;
                data["LocTest Proof"] = "LocaleTest transformer WORKS on SPT 4.1";
                data["Offline raid test mode"] += " [LocTest]";
                return data;
            });
            logger.Info("LocaleTest: transformer 已注册到 'en'");
        }
        else
        {
            logger.Warning("LocaleTest: 找不到 'en' locale");
        }

        // 立即读回验证（transformer 在 Value getter 时应用）
        try
        {
            var en = localeTable.Global["en"].Value;
            if (en is not null && en.TryGetValue("LocTest Proof", out var proof))
            {
                logger.Success($"LocaleTest 验证成功: '{proof}'");
            }
            else
            {
                logger.Error("LocaleTest 验证失败: 找不到 LocTest Proof key");
            }
        }
        catch (Exception ex)
        {
            logger.Error($"LocaleTest 读回异常: {ex.Message}");
        }

        return Task.CompletedTask;
    }
}

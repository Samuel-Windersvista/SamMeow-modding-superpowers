namespace SamMeow.LLMTraderChat.Server.Config;

/// <summary>
/// 配置装载结果：解析后的配置对象 + 装载过程中产生的告警列表。
///
/// 告警用于「响亮日志」：每条都带来源文件与字段路径（如 <c>config.jsonc: 字段 traders[0].traderId …</c>），
/// 供入口在启动时逐条以 Warning 级别输出，便于诊断。
/// </summary>
public sealed record ConfigLoadResult(ModConfig Config, IReadOnlyList<string> Warnings);

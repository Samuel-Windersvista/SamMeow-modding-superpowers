namespace InescapableTarkovsSoftcore.Config;

/// <summary>
/// 配置加载结果：解析后的配置对象 + 解析过程中产生的告警列表
/// （未知键、类型非法、文件缺失后重建等）。
/// </summary>
public sealed record ConfigLoadResult(SoftcoreConfig Config, IReadOnlyList<string> Warnings);

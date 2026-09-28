using System.Text.Json.Serialization;

namespace {{ROOT_NAMESPACE}}.Config;

/// <summary>
/// Mod 私有配置（纯 POCO）。
/// STD-CFG-004：配置类禁止标注 [Injectable]——否则容器会用默认值新建实例，
/// 磁盘上的 config.jsonc 永远不会被读取；配置只能经 IOnDIConstruct + AddSingleton 注册。
/// STD-CFG-002：玩家可改的运行时配置放在 mod 根目录的 config/config.jsonc（.jsonc 允许注释）。
/// </summary>
public record {{MOD_CLASS_NAME}}Config
{
    /// <summary>总开关（示例）。</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>数值配置示例。</summary>
    [JsonPropertyName("exampleMultiplier")]
    public float ExampleMultiplier { get; set; } = 1f;

    /// <summary>字符串列表配置示例。</summary>
    [JsonPropertyName("exampleTags")]
    public List<string> ExampleTags { get; set; } = [];
}

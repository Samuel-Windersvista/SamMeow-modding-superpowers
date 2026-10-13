using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Config;

/// <summary>
/// 根配置：general 总开关 + 各功能组开关 + 战局时长。
/// 键名与 <c>config/default-config.json</c> 一一对应（一致性由测试守护）。
/// </summary>
public sealed class SoftcoreConfig
{
    [JsonPropertyName("general")]
    public GeneralConfig General { get; set; } = new();

    [JsonPropertyName("samuelTweaks")]
    public SamuelTweaksConfig SamuelTweaks { get; set; } = new();

    [JsonPropertyName("trueItems")]
    public TrueItemsConfig TrueItems { get; set; } = new();

    [JsonPropertyName("noFirHideout")]
    public NoFirHideoutConfig NoFirHideout { get; set; } = new();

    [JsonPropertyName("antigravArmbands")]
    public AntigravArmbandsConfig AntigravArmbands { get; set; } = new();

    [JsonPropertyName("backpacks")]
    public BackpacksConfig Backpacks { get; set; } = new();

    [JsonPropertyName("softcore")]
    public SoftcoreModuleConfig Softcore { get; set; } = new();

    [JsonPropertyName("raidDuration")]
    public RaidDurationConfig RaidDuration { get; set; } = new();
}

/// <summary>全局配置段：沿用模块段开关，另加 debug。</summary>
public sealed class GeneralConfig : ModuleConfig
{
    /// <summary>调试模式。</summary>
    [JsonPropertyName("debug")]
    public bool Debug { get; set; }
}

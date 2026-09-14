using BepInEx.Configuration;

namespace {{ROOT_NAMESPACE}}.Client;

/// <summary>
/// BepInEx 配置封装：在 Plugin.Awake 中创建，绑定 BaseUnityPlugin.Config（ConfigFile）。
/// STD-CFG-006：客户端配置必须经 BaseUnityPlugin.Config 的 Config.Bind 声明，
///              运行时落在 BepInEx/config/&lt;ModGuid&gt;.cfg；不要自建 JSON 配置读取。
/// 玩家可手改 .cfg，或用 ConfigurationManager 插件改。
/// </summary>
// STD-CFG-006
public class {{MOD_CLASS_NAME}}Configuration
{
    /// <summary>总开关示例</summary>
    public ConfigEntry<bool> Enabled { get; }

    /// <summary>数值配置示例（带取值范围）</summary>
    public ConfigEntry<float> ExampleMultiplier { get; }

    public {{MOD_CLASS_NAME}}Configuration(ConfigFile configFile)
    {
        Enabled = configFile.Bind(
            "General",          // 分区名
            "Enabled",          // 键名
            true,               // 默认值
            "是否启用本 mod（示例配置项）");

        ExampleMultiplier = configFile.Bind(
            "General",
            "ExampleMultiplier",
            1f,
            new ConfigDescription(
                "示例数值配置项",
                new AcceptableValueRange<float>(0f, 10f)));
    }
}

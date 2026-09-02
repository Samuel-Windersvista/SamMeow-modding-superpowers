using BepInEx.Configuration;

namespace {{ROOT_NAMESPACE}};

/// <summary>
/// BepInEx 配置封装：在 Plugin.Awake 中创建，绑定 BaseUnityPlugin.Config（ConfigFile）。
/// 配置写入 BepInEx/config/&lt;GUID&gt;.cfg，玩家可手改或用 ConfigurationManager 插件改。
/// </summary>
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

using BepInEx.Configuration;

namespace PassClient;

// STD-CFG-006：客户端配置必须经 Config.Bind 声明
public class PassClientConfiguration
{
    public ConfigEntry<bool> Enabled { get; }

    public PassClientConfiguration(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "fixture 开关");
    }
}

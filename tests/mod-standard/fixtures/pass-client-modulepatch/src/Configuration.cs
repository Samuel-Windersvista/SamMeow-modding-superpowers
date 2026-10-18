using BepInEx.Configuration;

namespace PassClientModulePatch;

// STD-CFG-006：客户端配置必须经 Config.Bind 声明
public class PassModulePatchConfiguration
{
    public ConfigEntry<bool> Enabled { get; }

    public PassModulePatchConfiguration(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "fixture 开关");
    }
}

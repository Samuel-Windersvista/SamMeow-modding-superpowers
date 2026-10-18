using BepInEx.Configuration;

namespace FailClientModulePatch;

// STD-CFG-006：客户端配置必须经 Config.Bind 声明
public class FailModulePatchConfiguration
{
    public ConfigEntry<bool> Enabled { get; }

    public FailModulePatchConfiguration(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "fixture 开关");
    }
}

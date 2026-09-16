using BepInEx.Configuration;

namespace FourPartClient;

public class FourPartClientConfiguration
{
    public ConfigEntry<bool> Enabled { get; }

    public FourPartClientConfiguration(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "fixture 开关");
    }
}

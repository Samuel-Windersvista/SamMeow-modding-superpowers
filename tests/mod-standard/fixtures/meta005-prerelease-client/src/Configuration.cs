using BepInEx.Configuration;

namespace PrereleaseClient;

public class PrereleaseClientConfiguration
{
    public ConfigEntry<bool> Enabled { get; }

    public PrereleaseClientConfiguration(ConfigFile config)
    {
        Enabled = config.Bind("General", "Enabled", true, "fixture 开关");
    }
}

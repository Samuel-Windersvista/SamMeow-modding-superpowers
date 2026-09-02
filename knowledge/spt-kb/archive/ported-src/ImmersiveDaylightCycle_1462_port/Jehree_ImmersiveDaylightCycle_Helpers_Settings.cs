using BepInEx.Configuration;

namespace Jehree.ImmersiveDaylightCycle.Helpers;

internal class Settings
{
	public static ConfigEntry<bool> ModEnabled;

	public static ConfigEntry<bool> FactoryTimeAlwaysSelectable;

	public static void Init(ConfigFile config)
	{
		ModEnabled = config.Bind<bool>("1: Mod", "Mod Enabled", true, "Enables and disables the mod (wow woah crazy bro INSANE).");
		FactoryTimeAlwaysSelectable = config.Bind<bool>("3: Factory", "Factory Time Always Selectable", false, "Enable this to make Factory always have selectable day or night time.");
	}
}

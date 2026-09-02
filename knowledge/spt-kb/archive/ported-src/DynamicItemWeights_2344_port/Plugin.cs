using BepInEx;
using BepInEx.Configuration;
using Tosox.DynamicItemWeights.Helpers;
using Tosox.DynamicItemWeights.Patches;
using System.IO;
using System.Reflection;

namespace Tosox.DynamicItemWeights
{
    [BepInPlugin("de.tosox.dynamicitemweights", PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginName = "Dynamic Item Weights";
        public const string PluginVersion = "1.1.0";

        public static readonly string PluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static readonly string ConfigFolder = Path.Combine(PluginFolder, Assembly.GetExecutingAssembly().GetName().Name);

        internal static ConfigEntry<bool> IsPluginEnabled;
        internal static ConfigEntry<float> DefaultEmptyFraction;

        public void Awake()
        {
            IsPluginEnabled = Config.Bind("General", "Enable Plugin", true, "Untick to disable dynamic item weights");
            DefaultEmptyFraction = Config.Bind("Tuning", "Default Empty Fraction", 0.125f, "Fallback empty weight as fraction of original weight");

            UsageHelper usageHelper = new UsageHelper(Logger);
            EmptyWeightDB emptyWeightDB = new EmptyWeightDB(Logger);

            if (!emptyWeightDB.Load())
            {
                Logger.LogWarning("Plugin will be disabled");
                return;
            }

            new ItemWeightPatch(usageHelper, emptyWeightDB).Enable();

            Logger.LogInfo("Plugin loaded successfully");
        }
    }
}

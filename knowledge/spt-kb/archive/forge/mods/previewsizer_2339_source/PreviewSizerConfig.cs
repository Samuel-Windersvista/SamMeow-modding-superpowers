using acidphantasm_previewsizer;
using BepInEx.Configuration;
using System;

namespace acidphantasm_previewsizer
{
    internal static class PreviewSizerConfig
    {
        private static int loadOrder = 100;
        private const string ConfigGeneral = "1. General";
        public static ConfigEntry<bool> _actionPanel;
        public static ConfigEntry<int> _previewMinHeight;


        public static void InitConfig(ConfigFile config)
        {
            // General Settings
            _actionPanel = config.Bind(ConfigGeneral, "Disable Actions Panel of Item Window", true, new ConfigDescription("Enable or disable the action panel of the window.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            _previewMinHeight = config.Bind(ConfigGeneral, "Item Preview Window Height", 200, new ConfigDescription("Changes the height of the item preview section of the window.", new AcceptableValueRange<int>(10, 350), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            
            // Triggers
            _actionPanel.SettingChanged += SettingChanged;
            _previewMinHeight.SettingChanged += SettingChanged;

            // Set Them Initially
            Plugin._actionPanel = _actionPanel.Value;
            Plugin._previewMinHeight = _previewMinHeight.Value;
        }

        private static void SettingChanged(object sender, EventArgs e)
        {
            Plugin._actionPanel = _actionPanel.Value;
            Plugin._previewMinHeight = _previewMinHeight.Value;
        }
    }
}
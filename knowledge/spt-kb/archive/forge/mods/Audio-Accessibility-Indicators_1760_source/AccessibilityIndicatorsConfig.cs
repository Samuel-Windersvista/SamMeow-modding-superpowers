using acidphantasm_accessibilityindicators.IndicatorUI;
using BepInEx.Configuration;
using System;
using UnityEngine;

namespace acidphantasm_accessibilityindicators
{
    internal static class AccessibilityIndicatorsConfig
    {
        private static int loadOrder = 100;
        private const string ConfigGeneral = "1. General";
        public static ConfigEntry<bool> enable;
        public static ConfigEntry<bool> showTeammates;
        public static ConfigEntry<bool> normalizeDistance;
        public static ConfigEntry<float> maxNormalizedDistance;
        public static ConfigEntry<float> minNormalizedDistance;
        public static ConfigEntry<float> indicatorOffset;

        private const string ConfigShotsTitle = "2. Shots Configuration";
        public static ConfigEntry<bool> enableShots;
        public static ConfigEntry<float> maxShotDistance;
        public static ConfigEntry<float> fadeTimeShots;
        public static ConfigEntry<Color> enemyShotColour;
        public static ConfigEntry<Color> friendShotColour;

        private const string ConfigSprintTitle = "3. Sprint Configuration";
        public static ConfigEntry<bool> enableSprintSteps;
        public static ConfigEntry<float> maxSprintDistance;
        public static ConfigEntry<float> fadeTimeSprint;
        public static ConfigEntry<Color> enemySprintColour;
        public static ConfigEntry<Color> friendSprintColour;

        private const string ConfigWalkTitle = "4. Walk Configuration";
        public static ConfigEntry<bool> enableWalkSteps;
        public static ConfigEntry<float> maxWalkDistance;
        public static ConfigEntry<float> fadeTimeWalk;
        public static ConfigEntry<Color> enemyWalkColour;
        public static ConfigEntry<Color> friendWalkColour;

        private const string ConfigSneakTitle = "5. Sneak Configuration";
        public static ConfigEntry<bool> enableSneakSteps;
        public static ConfigEntry<float> maxSneakDistance;
        public static ConfigEntry<float> fadeTimeSneak;
        public static ConfigEntry<Color> enemySneakColour;
        public static ConfigEntry<Color> friendSneakColour;

        private const string ConfigVoiceTitle = "6. Voiceline Configuration";
        public static ConfigEntry<bool> enableVoicelines;
        public static ConfigEntry<float> maxVoiceDistance;
        public static ConfigEntry<float> fadeTimeVoice;

        private const string ConfigAdvancedTitle = "7. Advanced";
        public static ConfigEntry<int> poolObjectsShots;
        public static ConfigEntry<int> poolObjectsSteps;
        public static ConfigEntry<int> poolObjectsVoice;
        public static ConfigEntry<int> poolObjectsVerticality;


        public static void InitAAConfig(ConfigFile config)
        {
            // General Settings
            enable = config.Bind(ConfigGeneral, "Enable Mod", true, new ConfigDescription("Enable or disable this mod.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            showTeammates = config.Bind(ConfigGeneral, "Show Teammates", false, new ConfigDescription("If you enjoy Swedish Coffee, this toggle is for you. Toggle showing indicators for teammates.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            normalizeDistance = config.Bind(ConfigGeneral, "Normalize Indicator Size", true, new ConfigDescription("Toggles normalizing the size of the indicator based on distance. Disabled means each indicator type will size respective to it's configured max distance, enabled means all indicator types will be the same size at the same distance.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            minNormalizedDistance = config.Bind(ConfigGeneral, "Min Normalized Distance", 1f, new ConfigDescription("Indicator will be it's largest when the sound made is this close (or closer) to you.", new AcceptableValueRange<float>(0.1f, 24f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            maxNormalizedDistance = config.Bind(ConfigGeneral, "Max Normalized Distance", 75f, new ConfigDescription("Indicator will be it's smallest when the sound made is this far (or further) from you.", new AcceptableValueRange<float>(25f, 300f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            indicatorOffset = config.Bind(ConfigGeneral, "Indicator Offset", 25f, new ConfigDescription("Indicator offset for the UI, higher value moves it further from the center of screen - lower moves closer to center.", new AcceptableValueRange<float>(-50f, 100f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            Indicators.enable = enable.Value;
            Indicators.showTeammates = showTeammates.Value;
            Indicators.normalizeDistance = normalizeDistance.Value;
            Indicators.minNormalizedDistance = minNormalizedDistance.Value;
            Indicators.maxNormalizedDistance = maxNormalizedDistance.Value;
            Indicators.indicatorOffset = indicatorOffset.Value;

            // Shots
            enableShots = config.Bind(ConfigShotsTitle, "Enable Shots", true, new ConfigDescription("Enable or disable shot indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            maxShotDistance = config.Bind(ConfigShotsTitle, "Max Distance", 150f, new ConfigDescription("Max distance from sound to show indicator.", new AcceptableValueRange<float>(1f, 500f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            fadeTimeShots = config.Bind(ConfigShotsTitle, "Fade Time", 1f, new ConfigDescription("Amount of time in seconds for indicator to fade.", new AcceptableValueRange<float>(0.25f, 2f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            enemyShotColour = config.Bind(ConfigShotsTitle, "Enemy Shot Colour", new Color(0.42f, 0.094f, 0.145f), new ConfigDescription("Colour for enemy shot indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            friendShotColour = config.Bind(ConfigShotsTitle, "Friendly Shot Colour", new Color(0.341f, 0.078f, 0.306f), new ConfigDescription("Colour for friendly shot indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            Indicators.enableShots = enableShots.Value;
            Indicators.maxShotDistance = maxShotDistance.Value;
            Indicators.fadeTimeShots = fadeTimeShots.Value;
            Indicators.enemyShotColour = enemyShotColour.Value;
            Indicators.friendShotColour = friendShotColour.Value;


            // Steps
            enableSprintSteps = config.Bind(ConfigSprintTitle, "Enable Sprint Steps", true, new ConfigDescription("Enable or disable sprint step indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            maxSprintDistance = config.Bind(ConfigSprintTitle, "Max Sprint Distance", 50f, new ConfigDescription("Max distance from sound to show indicator.", new AcceptableValueRange<float>(1f, 100f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            fadeTimeSprint = config.Bind(ConfigSprintTitle, "Sprint Fade Time", 0.75f, new ConfigDescription("Amount of time in seconds for indicator to fade.", new AcceptableValueRange<float>(0.25f, 2f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            enemySprintColour = config.Bind(ConfigSprintTitle, "Enemy Sprint Colour", new Color(0.078f, 0.259f, 0.341f), new ConfigDescription("Colour for enemy sprint indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            friendSprintColour = config.Bind(ConfigSprintTitle, "Friendly Sprint Colour", new Color(0.388f, 0.541f, 0.482f), new ConfigDescription("Colour for friendly sprint indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            Indicators.enableSprintSteps = enableSprintSteps.Value;
            Indicators.maxSprintDistance = maxSprintDistance.Value;
            Indicators.fadeTimeSprint = fadeTimeSprint.Value;
            Indicators.enemySprintColour = enemySprintColour.Value;
            Indicators.friendSprintColour = friendSprintColour.Value;

            enableWalkSteps = config.Bind(ConfigWalkTitle, "Enable Walk Steps", true, new ConfigDescription("Enable or disable walk step indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            maxWalkDistance = config.Bind(ConfigWalkTitle, "Max Walk Distance", 30f, new ConfigDescription("Max distance from sound to show indicator.", new AcceptableValueRange<float>(1f, 60f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            fadeTimeWalk = config.Bind(ConfigWalkTitle, "Walk Fade Time", 0.75f, new ConfigDescription("Amount of time in seconds for indicator to fade.", new AcceptableValueRange<float>(0.25f, 2f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            enemyWalkColour = config.Bind(ConfigWalkTitle, "Enemy Walk Colour", new Color(0.122f, 0.094f, 0.42f), new ConfigDescription("Colour for enemy Walk indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            friendWalkColour = config.Bind(ConfigWalkTitle, "Friendly Walk Colour", new Color(0.098f, 0.341f, 0.078f), new ConfigDescription("Colour for friendly Walk indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            Indicators.enableWalkSteps = enableWalkSteps.Value;
            Indicators.maxWalkDistance = maxWalkDistance.Value;
            Indicators.fadeTimeWalk = fadeTimeWalk.Value;
            Indicators.enemyWalkColour = enemyWalkColour.Value;
            Indicators.friendWalkColour = friendWalkColour.Value;

            enableSneakSteps = config.Bind(ConfigSneakTitle, "Enable Sneak Steps", true, new ConfigDescription("Enable or disable sneak step indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            maxSneakDistance = config.Bind(ConfigSneakTitle, "Max Sneak Distance", 15f, new ConfigDescription("Max distance from sound to show indicator.", new AcceptableValueRange<float>(1f, 25f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            fadeTimeSneak = config.Bind(ConfigSneakTitle, "Sneak Fade Time", 0.75f, new ConfigDescription("Amount of time in seconds for indicator to fade.", new AcceptableValueRange<float>(0.25f, 2f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            enemySneakColour = config.Bind(ConfigSneakTitle, "Enemy Sneak Colour", new Color(0.196f, 0.078f, 0.341f), new ConfigDescription("Colour for enemy sneak indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            friendSneakColour = config.Bind(ConfigSneakTitle, "Friendly Sneak Colour", new Color(0.322f, 0.341f, 0.078f), new ConfigDescription("Colour for friendly sneak indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            Indicators.enableSneakSteps = enableSneakSteps.Value;
            Indicators.maxSneakDistance = maxSneakDistance.Value;
            Indicators.fadeTimeSneak = fadeTimeSneak.Value;
            Indicators.enemySneakColour = enemySneakColour.Value;
            Indicators.friendSneakColour = friendSneakColour.Value;

            // Voice
            enableVoicelines = config.Bind(ConfigVoiceTitle, "Enable Voicelines", true, new ConfigDescription("Enable or disable voice indicators.", null, new ConfigurationManagerAttributes { Order = loadOrder-- }));
            maxVoiceDistance = config.Bind(ConfigVoiceTitle, "Max Voice Distance", 50f, new ConfigDescription("Max distance from sound to show indicator.", new AcceptableValueRange<float>(1f, 75f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            fadeTimeVoice = config.Bind(ConfigVoiceTitle, "Voice Fade Time", 1f, new ConfigDescription("Amount of time in seconds for indicator to fade.", new AcceptableValueRange<float>(0.25f, 2f), new ConfigurationManagerAttributes { Order = loadOrder-- }));
            Indicators.enableVoicelines = enableVoicelines.Value;
            Indicators.maxVoiceDistance = maxVoiceDistance.Value;
            Indicators.fadeTimeVoice = fadeTimeVoice.Value;

            // Advanced
            poolObjectsSteps = config.Bind(ConfigAdvancedTitle, "Shots Pool Objects", 15, new ConfigDescription("Number of indicator clones to make. Increase this if indicators are not showing when they should.", new AcceptableValueRange<int>(10, 100), new ConfigurationManagerAttributes { IsAdvanced = true, Order = loadOrder-- }));
            poolObjectsShots = config.Bind(ConfigAdvancedTitle, "Footstep Pool Objects", 15, new ConfigDescription("Number of indicator clones to make. Increase this if indicators are not showing when they should.", new AcceptableValueRange<int>(10, 100), new ConfigurationManagerAttributes { IsAdvanced = true, Order = loadOrder-- }));
            poolObjectsVoice = config.Bind(ConfigAdvancedTitle, "Voice Pool Objects", 15, new ConfigDescription("Number of indicator clones to make. Increase this if indicators are not showing when they should.", new AcceptableValueRange<int>(10, 100), new ConfigurationManagerAttributes { IsAdvanced = true, Order = loadOrder-- }));
            poolObjectsVerticality = config.Bind(ConfigAdvancedTitle, "Verticality Pool Objects", 15, new ConfigDescription("Number of indicator clones to make. Increase this if indicators are not showing when they should.", new AcceptableValueRange<int>(10, 100), new ConfigurationManagerAttributes { IsAdvanced = true, Order = loadOrder-- }));
            Panel.poolObjectsSteps = poolObjectsSteps.Value;
            Panel.poolObjectsShots = poolObjectsShots.Value;
            Panel.poolObjectsVoice = poolObjectsVoice.Value;
            Panel.poolObjectsVerticality = poolObjectsVerticality.Value;

            // Triggers
            enable.SettingChanged += Accessibility_SettingChanged;
            showTeammates.SettingChanged += Accessibility_SettingChanged;
            normalizeDistance.SettingChanged += Accessibility_SettingChanged;
            minNormalizedDistance.SettingChanged += Accessibility_SettingChanged;
            maxNormalizedDistance.SettingChanged += Accessibility_SettingChanged;
            indicatorOffset.SettingChanged += Accessibility_SettingChanged;

            enableShots.SettingChanged += Accessibility_SettingChanged;
            maxShotDistance.SettingChanged += Accessibility_SettingChanged;
            fadeTimeShots.SettingChanged += Accessibility_SettingChanged;
            enemyShotColour.SettingChanged += Accessibility_SettingChanged;
            friendShotColour.SettingChanged += Accessibility_SettingChanged;

            enableSprintSteps.SettingChanged += Accessibility_SettingChanged;
            maxSprintDistance.SettingChanged += Accessibility_SettingChanged;
            fadeTimeSprint.SettingChanged += Accessibility_SettingChanged;
            enemySprintColour.SettingChanged += Accessibility_SettingChanged;
            friendSprintColour.SettingChanged += Accessibility_SettingChanged;

            enableWalkSteps.SettingChanged += Accessibility_SettingChanged;
            maxWalkDistance.SettingChanged += Accessibility_SettingChanged;
            fadeTimeWalk.SettingChanged += Accessibility_SettingChanged;
            enemyWalkColour.SettingChanged += Accessibility_SettingChanged;
            friendWalkColour.SettingChanged += Accessibility_SettingChanged;

            enableSneakSteps.SettingChanged += Accessibility_SettingChanged;
            maxSneakDistance.SettingChanged += Accessibility_SettingChanged;
            fadeTimeSneak.SettingChanged += Accessibility_SettingChanged;
            enemySneakColour.SettingChanged += Accessibility_SettingChanged;
            friendSneakColour.SettingChanged += Accessibility_SettingChanged;

            enableVoicelines.SettingChanged += Accessibility_SettingChanged;
            maxVoiceDistance.SettingChanged += Accessibility_SettingChanged;
            fadeTimeVoice.SettingChanged += Accessibility_SettingChanged;

            poolObjectsShots.SettingChanged += Accessibility_SettingChanged;
            poolObjectsSteps.SettingChanged += Accessibility_SettingChanged;
            poolObjectsVoice.SettingChanged += Accessibility_SettingChanged;
            poolObjectsVerticality.SettingChanged += Accessibility_SettingChanged;
        }

        private static void Accessibility_SettingChanged(object sender, EventArgs e)
        {
            Indicators.enable = enable.Value;
            Indicators.showTeammates = showTeammates.Value;
            Indicators.normalizeDistance = normalizeDistance.Value;
            Indicators.minNormalizedDistance = minNormalizedDistance.Value;
            Indicators.maxNormalizedDistance = maxNormalizedDistance.Value;
            Indicators.indicatorOffset = indicatorOffset.Value;

            Indicators.enableShots = enableShots.Value;
            Indicators.maxShotDistance = maxShotDistance.Value;
            Indicators.fadeTimeShots = fadeTimeShots.Value;
            Indicators.enemyShotColour = enemyShotColour.Value;
            Indicators.friendShotColour = friendShotColour.Value;


            Indicators.enableSprintSteps = enableSprintSteps.Value;
            Indicators.maxSprintDistance = maxSprintDistance.Value;
            Indicators.fadeTimeSprint = fadeTimeSprint.Value;
            Indicators.enemySprintColour = enemySprintColour.Value;
            Indicators.friendSprintColour = friendSprintColour.Value;

            Indicators.enableWalkSteps = enableWalkSteps.Value;
            Indicators.maxWalkDistance = maxWalkDistance.Value;
            Indicators.fadeTimeWalk = fadeTimeWalk.Value;
            Indicators.enemyWalkColour = enemyWalkColour.Value;
            Indicators.friendWalkColour = friendWalkColour.Value;

            Indicators.enableSneakSteps = enableSneakSteps.Value;
            Indicators.maxSneakDistance = maxSneakDistance.Value;
            Indicators.fadeTimeSneak = fadeTimeSneak.Value;
            Indicators.enemySneakColour = enemySneakColour.Value;
            Indicators.friendSneakColour = friendSneakColour.Value;

            Indicators.enableVoicelines = enableVoicelines.Value;
            Indicators.maxVoiceDistance = maxVoiceDistance.Value;
            Indicators.fadeTimeVoice = fadeTimeVoice.Value;

            Panel.poolObjectsSteps = poolObjectsSteps.Value;
            Panel.poolObjectsShots = poolObjectsShots.Value;
            Panel.poolObjectsVoice = poolObjectsVoice.Value;
            Panel.poolObjectsVerticality = poolObjectsVerticality.Value;
        }
    }
}
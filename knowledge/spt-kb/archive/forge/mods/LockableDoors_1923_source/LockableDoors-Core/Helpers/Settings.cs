using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using System.Collections.Generic;

namespace LockableDoors.Helpers
{
    internal class Settings
    {
        public static ConfigEntry<bool> VisualizerEnabled;
        public static ConfigEntry<float> NavMeshObstacleXSizeMultiplier;
        public static ConfigEntry<float> NavMeshObstacleYSizeMultiplier;
        public static ConfigEntry<float> NavMeshObstacleZSizeMultiplier;

        public static ConfigEntry<bool> LockedDoorLimitsEnabled;
        public static ConfigEntry<bool> BSGLockedDoorsRequireKey;
        public static ConfigEntry<bool> UnlockAllDoorsOnRaidEnd;
        public static ConfigEntry<bool> OldLockFalloff;

        public static ConfigEntry<int> CustomsAllottedPoints;
        public static ConfigEntry<int> FactoryAllottedPoints;
        public static ConfigEntry<int> InterchangeAllottedPoints;
        public static ConfigEntry<int> LabAllottedPoints;
        public static ConfigEntry<int> LighthouseAllottedPoints;
        public static ConfigEntry<int> ReserveAllottedPoints;
        public static ConfigEntry<int> GroundZeroAllottedPoints;
        public static ConfigEntry<int> ShorelineAllottedPoints;
        public static ConfigEntry<int> StreetsAllottedPoints;
        public static ConfigEntry<int> WoodsAllottedPoints;

        private const string _section1Name = "2: Number of Locked Doors Per Map";
        private const string _section1Description = "Maximum number of doors that can be locked on this map.";
        private static Dictionary<string, ConfigEntry<int>> _itemCountLookup = new();

        public static void Init(ConfigFile config)
        {
            VisualizerEnabled = config.Bind(
                "0: Advanced",
                "Visualizer Enabled",
                false,
                new ConfigDescription("Enable Nav Mesh Obstacle visualizer.", null, new ConfigurationManagerAttributes { IsAdvanced = true })
            );
            NavMeshObstacleXSizeMultiplier = config.Bind(
                "0: Advanced",
                "Nav Mesh Obstacle X Size Multiplier",
                1.5f,
                new ConfigDescription("Sets the X size multiplier for the Nav Mesh Obstacle.", null, new ConfigurationManagerAttributes { IsAdvanced = true })
            );
            NavMeshObstacleYSizeMultiplier = config.Bind(
                "0: Advanced",
                "Nav Mesh Obstacle Y Size Multiplier",
                1.5f,
                new ConfigDescription("Sets the Y size multiplier for the Nav Mesh Obstacle.", null, new ConfigurationManagerAttributes { IsAdvanced = true })
            );
            NavMeshObstacleZSizeMultiplier = config.Bind(
                "0: Advanced",
                "Nav Mesh Obstacle Z Size Multiplier",
                2.5f,
                new ConfigDescription("Sets the Z size multiplier for the Nav Mesh Obstacle.", null, new ConfigurationManagerAttributes { IsAdvanced = true })
            );

            LockedDoorLimitsEnabled = config.Bind(
                "1: General",
                "Limits Enabled",
                true,
                "Set to false if you want unlimited locked doors on every map."
            );
            BSGLockedDoorsRequireKey = config.Bind(
                "1: General",
                "BSG Locked Doors Require Key",
                false,
                "Set to false if you want to be able to re-unlock doors that require a key without consuming another use of the key. Recommended to leave disabled unless you have infinite key uses enabled via another mod. This setting requires a new raid to take full affect."
            );
            UnlockAllDoorsOnRaidEnd = config.Bind(
                "1: General",
                "Unlock All Doors On Raid End",
                false,
                "Automatically unlock all doors when the raid ends."
            );
            OldLockFalloff = config.Bind(
                "1: General",
                "Old Lock Falloff",
                false,
                "If enabled, and when locked doors are maxed out, locking another door will cause the oldest locked one to unlock. Allows for 'circular' locking where you don't have to go back and unlock old doors to lock new ones."
            );

            CustomsAllottedPoints = config.Bind(
                _section1Name,
                "Customs",
                4,
                _section1Description
            );
            FactoryAllottedPoints = config.Bind(
                _section1Name,
                "Factory",
                2,
                _section1Description
            );
            InterchangeAllottedPoints = config.Bind(
                _section1Name,
                "Interchange",
                4,
                _section1Description
            );
            LabAllottedPoints = config.Bind(
                _section1Name,
                "Lab",
                1,
                _section1Description
            );
            LighthouseAllottedPoints = config.Bind(
                _section1Name,
                "Lighthouse",
                6,
                _section1Description
            );
            ReserveAllottedPoints = config.Bind(
                _section1Name,
                "Reserve",
                4,
                _section1Description
            );
            GroundZeroAllottedPoints = config.Bind(
                _section1Name,
                "Ground Zero",
                3,
                _section1Description
            );
            ShorelineAllottedPoints = config.Bind(
                _section1Name,
                "Shoreline",
                4,
                _section1Description
            );
            StreetsAllottedPoints = config.Bind(
                _section1Name,
                "Streets",
                6,
                _section1Description
            );
            WoodsAllottedPoints = config.Bind(
                _section1Name,
                "Woods",
                4,
                _section1Description
            );

            _itemCountLookup.Add("bigmap", CustomsAllottedPoints);
            _itemCountLookup.Add("factory4_day", FactoryAllottedPoints);
            _itemCountLookup.Add("factory4_night", FactoryAllottedPoints);
            _itemCountLookup.Add("interchange", InterchangeAllottedPoints);
            _itemCountLookup.Add("laboratory", LabAllottedPoints);
            _itemCountLookup.Add("lighthouse", LighthouseAllottedPoints);
            _itemCountLookup.Add("rezervbase", ReserveAllottedPoints);
            _itemCountLookup.Add("sandbox", GroundZeroAllottedPoints);
            _itemCountLookup.Add("sandbox_high", GroundZeroAllottedPoints);
            _itemCountLookup.Add("shoreline", ShorelineAllottedPoints);
            _itemCountLookup.Add("tarkovstreets", StreetsAllottedPoints);
            _itemCountLookup.Add("woods", WoodsAllottedPoints);
        }

        public static int GetLockedDoorLimit()
        {
            if (!LockedDoorLimitsEnabled.Value)
            {
                return 99999999;
            }
            return _itemCountLookup[Singleton<GameWorld>.Instance.LocationId.ToLower()].Value;
        }
    }
}

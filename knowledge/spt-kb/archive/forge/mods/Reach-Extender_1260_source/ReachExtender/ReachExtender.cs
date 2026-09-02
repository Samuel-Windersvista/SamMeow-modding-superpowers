using BepInEx;
using BepInEx.Configuration;
using ReachExtender.Patches;
using UnityEngine;

namespace ReachExtender {
    [BepInPlugin("Mattdokn.ReachExtender", "ReachExtender", "1.3.0")]
    public class ReachExtenderPlugin : BaseUnityPlugin {

        public static ConfigEntry<float> InteractionDistance;
        public static ConfigEntry<float> DoorInteractionDistance;
        public static ConfigEntry<float> BelowFloorMaxLootDistance;
        public static float DistanceFromWallSquared;

        public static ConfigEntry<float> SphereCastRadius;
        public static ConfigEntry<bool> ShowSphereCastDebug;

        public static GameObject Sphere;

        void Awake() {
            InteractionDistance = Config.Bind("Loot Distances", "Loot/Container Interaction Distance", 2f, new ConfigDescription("Default Value is 1.3", new AcceptableValueRange<float>(0f, 20f)));
            DoorInteractionDistance = Config.Bind("Door Distances", "Door Interaction Distance", 1f, new ConfigDescription("Default Value is 1", new AcceptableValueRange<float>(0f, 5f)));
            BelowFloorMaxLootDistance = Config.Bind("Loot Distances", "How far to reach below floors to loot.", 0.75f, new ConfigDescription("Set to 0 to disable feature.", new AcceptableValueRange<float>(0f, 5f)));
            SphereCastRadius = Config.Bind("Sphere Casting", "Sphere Cast Radius.", 0.1f, new ConfigDescription("Set to 0 to disable feature.", new AcceptableValueRange<float>(0f, 2f)));
            ShowSphereCastDebug = Config.Bind("Sphere Casting", "Show debug sphere.", false, new ConfigDescription("Shows the radius of the sphere used for casting."));

            EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE = InteractionDistance.Value;
            EFTHardSettings.Instance.DOOR_RAYCAST_DISTANCE = DoorInteractionDistance.Value;
            DistanceFromWallSquared = BelowFloorMaxLootDistance.Value * BelowFloorMaxLootDistance.Value;

            Config.SettingChanged += Config_SettingChanged;

            new InteractionRaycastPatch().Enable();
        }

        private void Config_SettingChanged(object sender, SettingChangedEventArgs e) {
            EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE = InteractionDistance.Value;
            EFTHardSettings.Instance.DOOR_RAYCAST_DISTANCE = DoorInteractionDistance.Value;
            DistanceFromWallSquared = BelowFloorMaxLootDistance.Value * BelowFloorMaxLootDistance.Value;
        }
    }
}

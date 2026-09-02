using BepInEx.Logging;
using BepInEx;
using System;
using acidphantasm_accessibilityindicators.Patches;
using acidphantasm_accessibilityindicators.IndicatorUI;
using System.IO;
using UnityEngine;
using System.Reflection;

namespace acidphantasm_accessibilityindicators
{
    [BepInPlugin("com.acidphantasm.accessibilityindicators", "acidphantasm-AccessibilityIndicators", "2.0.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static Plugin Instance;

        internal void Awake()
        {
            LogSource = Logger;

            LogSource.LogInfo("[AccessibilityIndicators] loading...");

            if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }

            Instance = this;
            DontDestroyOnLoad(this);

            AccessibilityIndicatorsConfig.InitAAConfig(Config);

            new GameWorldOnGameStartedPatch().Enable();
            new GameWorldUnregisterPlayerPatch().Enable();
            new LevelSettingsPatch().Enable();
            new FirearmControllerPatch().Enable();
            new PhraseSpeakerClassPatch().Enable();
            new PlayerDefaultPlayPatch().Enable();
            new PlayerPlayStepSoundPatch().Enable();
            new PlayerMethod61Patch().Enable();

            LogSource.LogInfo("[AccessibilityIndicators] loaded!");
        }

        private void Start()
        {
            LoadBundle();
        }

        public static void LoadBundle()
        {
            var directory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var assetBundle = Path.Combine(directory, "assets", "accessibilityindicators.bundle");
            var bundle = AssetBundle.LoadFromFile(assetBundle);
            if (bundle == null)
            {
                throw new Exception($"Error loading bundles");
            }
            Panel.IndicatorHUDPrefab = LoadAsset<GameObject>(bundle, "Canvas.prefab");
            Panel.ShotPivotPrefab = LoadAsset<GameObject>(bundle, "shotPivot.prefab");
            Panel.StepPivotPrefab = LoadAsset<GameObject>(bundle, "runPivot.prefab");
            Panel.VoicePivotPrefab = LoadAsset<GameObject>(bundle, "voicePivot.prefab");
            Panel.VerticalityPivotPrefab = LoadAsset<GameObject>(bundle, "verticalityPivot.prefab");
        }
        private static T LoadAsset<T>(AssetBundle bundle, string assetPath) where T : UnityEngine.Object
        {
            T asset = bundle.LoadAsset<T>(assetPath);

            if (asset == null)
            {
                throw new Exception($"Error loading asset {assetPath}");
            }

            DontDestroyOnLoad(asset);
            return asset;
        }
    }
}

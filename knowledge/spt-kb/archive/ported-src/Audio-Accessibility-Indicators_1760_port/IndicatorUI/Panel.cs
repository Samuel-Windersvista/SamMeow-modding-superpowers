using acidphantasm_accessibilityindicators.Helpers;
using acidphantasm_accessibilityindicators.Scripts;
using UnityEngine;

namespace acidphantasm_accessibilityindicators.IndicatorUI
{
    internal class Panel : MonoBehaviour
    {
        public static GameObject IndicatorHUDPrefab;

        public static GameObject ShotPivotPrefab;
        public static GameObject StepPivotPrefab;
        public static GameObject VoicePivotPrefab;
        public static GameObject VerticalityPivotPrefab;

        public static GameObject IndicatorHUD;
        public static GameObject HUDCenterPoint;

        public static Vector3 northVector;
        public static float northDirection;

        public static int poolObjectsShots;
        public static int poolObjectsSteps;
        public static int poolObjectsVoice;
        public static int poolObjectsVerticality;

        private static KeepNorthRotation keepNorthRotationScript;

        public static void CreateHUD()
        {
            if (IndicatorHUD != null) return;

            IndicatorHUD = Instantiate(IndicatorHUDPrefab);
            HUDCenterPoint = IndicatorHUD.transform.GetChild(0).gameObject;
            ObjectPool.PoolShotIndicators(ShotPivotPrefab, HUDCenterPoint, poolObjectsShots);
            ObjectPool.PoolStepIndicators(StepPivotPrefab, HUDCenterPoint, poolObjectsSteps);
            ObjectPool.PoolVoiceIndicators(VoicePivotPrefab, HUDCenterPoint, poolObjectsVoice);
            ObjectPool.PoolVerticalityIndicators(VerticalityPivotPrefab, HUDCenterPoint, poolObjectsVerticality);
            IndicatorHUD.AddComponent<KeepNorthRotation>();
            keepNorthRotationScript = IndicatorHUD.GetOrAddComponent<KeepNorthRotation>();
            Plugin.LogSource.LogInfo("[Accessibility Indicators] Creating HUD");
        }

        public static void Dispose()
        {
            Plugin.LogSource.LogInfo("[Accessibility Indicators] Cleaning up HUD");
            keepNorthRotationScript.Stop();
        }
    }
}

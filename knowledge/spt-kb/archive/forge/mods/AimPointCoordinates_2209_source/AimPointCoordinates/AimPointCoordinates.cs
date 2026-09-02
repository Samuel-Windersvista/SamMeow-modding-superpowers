using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace AimPointCoordinates
{
    [BepInPlugin("com.internetduce.aimpointcoordinates", "AimPoint Coordinates", "1.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<bool> showCoordinates;
        private ConfigEntry<KeyboardShortcut> toggleKey;

        private Vector3 aimPoint;
        private bool hasHit;

        private void Awake()
        {
            showCoordinates = Config.Bind("General", "ShowCoordinates", true, "ShowCoordinates");
            toggleKey = Config.Bind("General", "ToggleKey", new KeyboardShortcut(KeyCode.F10), "ToggleKey");
        }

        private void Update()
        {
            if (toggleKey.Value.IsDown())
            {
                showCoordinates.Value = !showCoordinates.Value;
            }

            if (!showCoordinates.Value)
                return;

            if (!Singleton<GameWorld>.Instantiated || Camera.main == null)
                return;

            var ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, LayerMaskClass.HighPolyWithTerrainMask))
            {
                aimPoint = hit.point;
                hasHit = true;
            }
            else
            {
                hasHit = false;
            }
        }

        private void OnGUI()
        {
            if (!showCoordinates.Value)
                return;

            string text = hasHit
                ? $"Aim: X:{aimPoint.x:F2} Y:{aimPoint.y:F2} Z:{aimPoint.z:F2}"
                : "Aim: None";

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = hasHit ? Color.cyan : Color.red }
            };

            Vector2 size = style.CalcSize(new GUIContent(text));
            float x = (Screen.width - size.x) / 2;
            float y = (Screen.height / 2) + 20;

            GUI.Label(new Rect(x, y, size.x, size.y), text, style);
        }
    }
}

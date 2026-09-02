using HarmonyLib;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace DebugTooltip
{
    internal class TooltipCopier : MonoBehaviour
    {
        private string shortText;
        private string altShortText;
        private string longText;

        private static PropertyInfo SystemCopyBufferProperty;

        public void Awake()
        {
            SystemCopyBufferProperty = AccessTools.Property(typeof(GUIUtility), "systemCopyBuffer");
        }

        public void SetDebugInfo(DebugInfo debugInfo)
        {
            this.shortText = StripTags(debugInfo.ToShortString());
            this.altShortText = StripTags(debugInfo.ToAltShortString());
            this.longText = StripTags(debugInfo.ToString());
        }

        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                var ctrlDown = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftControl);
                var shiftDown = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                var altDown = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

                if (ctrlDown && shiftDown)
                {
                    SystemCopyBufferProperty.SetValue(null, longText, null);
                    return;
                }

                if (ctrlDown)
                {
                    SystemCopyBufferProperty.SetValue(null, shortText, null);
                }

                if (altDown)
                {
                    SystemCopyBufferProperty.SetValue(null, altShortText, null);
                }
            }
        }

        private string StripTags(string input)
        {
            return Regex.Replace(input, "</?color[^>]*>", string.Empty);
        }
    }
}

using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace DebugTooltip
{
    internal class ShortcutPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Update));
        }

        [PatchPostfix]
        public static void Postfix(ItemUiContext __instance)
        {
            if (Settings.ToggleShortcut.Value.IsDown())
            {
                Settings.ShowDebugInfo.Value = !Settings.ShowDebugInfo.Value;
            }
        }
    }
}

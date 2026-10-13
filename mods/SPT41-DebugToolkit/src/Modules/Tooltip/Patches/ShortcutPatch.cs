using System;
using System.Reflection;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.Tooltip.Patches
{
    /// <summary>
    /// 快捷键切换：借 ItemUiContext.Update 的每帧钩子读取 Toggle Shortcut（默认 F9），
    /// 免开 F12 实时切换 tooltip 调试信息。
    /// </summary>
    internal sealed class TooltipShortcutPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.Update));

        [PatchPostfix]
        private static void Postfix()
        {
            try
            {
                var toggleEntry = TooltipSettings.ToggleShortcut;
                var showEntry = TooltipSettings.ShowDebugInfo;
                if (toggleEntry == null || showEntry == null)
                {
                    return;
                }

                if (toggleEntry.Value.IsDown())
                {
                    showEntry.Value = !showEntry.Value;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] Toggle Shortcut 处理失败：{e.Message}");
            }
        }
    }
}

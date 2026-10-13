using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.Tooltip.Patches
{
    /// <summary>
    /// 商人卡片进入：显示 Trader ID。
    /// </summary>
    /// <remarks>
    /// 触点表 #31/#33：<c>TraderCard</c> 在 0.16.9 位于全局命名空间，显式接口方法名精确为
    /// <c>UnityEngine.EventSystems.IPointerEnterHandler.OnPointerEnter</c>（含命名空间点号）。
    /// <c>_trader</c> 在 0.16.9 为**私有**字段，故经 Harmony 参数注入 <c>____trader</c> 读取
    /// （5.0 线可直接访问 public 字段，此处不可照抄）。
    /// </remarks>
    internal sealed class TraderCardEnterPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(TraderCard), "UnityEngine.EventSystems.IPointerEnterHandler.OnPointerEnter");

        [PatchPostfix]
        private static void Postfix(Profile.TraderInfo ____trader)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                DebugTooltipState.SetDebugInfo(new TraderDebugInfo(____trader));
                TooltipHelpers.ShowTooltip(string.Empty);
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] TraderCard enter postfix 失败：{e.Message}");
            }
        }
    }

    /// <summary>商人卡片离开：关闭 tooltip。</summary>
    internal sealed class TraderCardExitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(TraderCard), "UnityEngine.EventSystems.IPointerExitHandler.OnPointerExit");

        [PatchPostfix]
        private static void Postfix()
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                TooltipHelpers.CloseTooltip();
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] TraderCard exit postfix 失败：{e.Message}");
            }
        }
    }
}

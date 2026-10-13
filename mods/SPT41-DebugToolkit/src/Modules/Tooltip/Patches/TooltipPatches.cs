using System;
using System.Reflection;
using System.Text;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using TMPro;
using UnityEngine;

namespace SamMeow.DebugToolkit.Modules.Tooltip.Patches
{
    /// <summary>
    /// 普通 tooltip（SimpleTooltip.Show）：prefix 把调试块追加进 tooltip 文本并取消显示延迟。
    /// </summary>
    /// <remarks>
    /// Mono 适配：0.16.9 无 IL2CPP 的 <c>Pointer</c>，实例同一性用引用比较
    /// <c>ReferenceEquals(__instance, ItemUiContext.Instance.Tooltip)</c>。
    /// </remarks>
    internal sealed class TooltipShowPatch : ModulePatch
    {
        private static TooltipCopier _copier;

        protected override MethodBase GetTargetMethod()
            => AccessTools.DeclaredMethod(typeof(SimpleTooltip), nameof(SimpleTooltip.Show));

        [PatchPrefix]
        private static void Prefix(SimpleTooltip __instance, ref string text, ref float delay)
        {
            // [CRITICAL] 补丁体绝不向游戏代码抛异常（异常穿透 = CTD）。
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                var context = ItemUiContext.Instance;
                if (context == null || __instance == null)
                {
                    return;
                }

                if (!ReferenceEquals(__instance, context.Tooltip))
                {
                    return;
                }

                DebugInfo debugInfo = DebugTooltipState.GetDebugInfo();
                if (debugInfo == null)
                {
                    return;
                }

                if (_copier == null)
                {
                    _copier = __instance.GetOrAddComponent<TooltipCopier>();
                }

                if (_copier == null)
                {
                    return;
                }

                _copier.SetDebugInfo(debugInfo);

                // 上游：「Ain't nobody got time for that」——调试块立即出现。
                delay = 0f;

                StringBuilder sb = new StringBuilder();
                if (!string.IsNullOrEmpty(text))
                {
                    sb.AppendLine();
                    sb.AppendLine();
                }

                sb.Append(debugInfo);

                if (TooltipSettings.ShowCopyPromptValue)
                {
                    TooltipHelpers.AppendCopyPrompts(sb, debugInfo);
                }

                text += sb.ToString();
                DebugTooltipState.Clear();
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] SimpleTooltip.Show prefix 失败：{e.Message}");
            }
        }
    }

    /// <summary>
    /// 价格 tooltip（PriceTooltip.Show）：prefix 克隆 <c>_price</c> 文本并写入调试块。
    /// </summary>
    internal sealed class PriceTooltipShowPatch : ModulePatch
    {
        private static TooltipCopier _copier;
        private static TextMeshProUGUI _debugText;

        protected override MethodBase GetTargetMethod()
            => AccessTools.DeclaredMethod(typeof(PriceTooltip), nameof(PriceTooltip.Show));

        [PatchPrefix]
        private static void Prefix(PriceTooltip __instance)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                var context = ItemUiContext.Instance;
                if (context == null || __instance == null)
                {
                    return;
                }

                if (!ReferenceEquals(__instance, context.PriceTooltip))
                {
                    return;
                }

                DebugInfo debugInfo = DebugTooltipState.GetDebugInfo();
                if (debugInfo == null)
                {
                    return;
                }

                if (_copier == null)
                {
                    _copier = __instance.GetOrAddComponent<TooltipCopier>();
                }

                if (_copier == null)
                {
                    return;
                }

                _copier.SetDebugInfo(debugInfo);

                if (_debugText == null)
                {
                    TextMeshProUGUI price = __instance._price;
                    if (price == null || price.transform.parent == null || price.transform.parent.parent == null)
                    {
                        return;
                    }

                    _debugText = UnityEngine.Object.Instantiate(price, price.transform.parent.parent, false);
                    _debugText.color = Color.white;
                }

                StringBuilder sb = new StringBuilder(debugInfo.ToString());
                if (TooltipSettings.ShowCopyPromptValue)
                {
                    TooltipHelpers.AppendCopyPrompts(sb, debugInfo);
                }

                _debugText.text = sb.ToString();
                DebugTooltipState.Clear();
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] PriceTooltip.Show prefix 失败：{e.Message}");
            }
        }

        /// <summary>
        /// 无条件销毁克隆的调试文本并置 null（模块停用 / 关闭开关时调用）。
        /// 空判 + 游戏对象销毁路径 fail-open。
        /// </summary>
        internal static void DestroyDebugText()
        {
            try
            {
                if (_debugText != null)
                {
                    UnityEngine.Object.Destroy(_debugText.gameObject);
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 销毁 PriceTooltip 调试文本失败：{e.Message}");
            }
            finally
            {
                _debugText = null;
            }
        }

        /// <summary>关闭调试信息时销毁克隆出来的调试文本（响应配置变化）。</summary>
        internal static void OnShowDebugInfoChanged()
        {
            bool show;
            try
            {
                show = TooltipSettings.ShowDebugInfoValue;
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 读取 Show Debug Tooltip 失败：{e.Message}");
                return;
            }

            if (!show)
            {
                DestroyDebugText();
            }
        }
    }
}

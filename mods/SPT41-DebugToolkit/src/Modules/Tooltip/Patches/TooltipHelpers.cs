using System;
using System.Text;
using EFT.UI;
using UnityEngine.EventSystems;

namespace SamMeow.DebugToolkit.Modules.Tooltip.Patches
{
    /// <summary>
    /// 各补丁共用的 Tooltip 适配工具：悬停事件订阅、tooltip 显示/关闭、复制提示拼接。
    /// </summary>
    /// <remarks>
    /// 4.1.6 = Mono：<see cref="HoverTrigger.OnHoverStart"/>/<see cref="HoverTrigger.OnHoverEnd"/>
    /// 是原生 <c>event Action&lt;PointerEventData&gt;</c>，可直接 <c>+=</c> / <c>-=</c>，
    /// 无需 IL2CPP 的 DelegateSupport 委托封送。
    /// 触点表 R1：退订登记直接走 <c>element.AddDisposable(...)</c>
    /// （0.16.9 的 <c>UIElement."UI"</c> 字段类型为 UIParent，无独立 UIContext 类型）。
    /// </remarks>
    internal static class TooltipHelpers
    {
        /// <summary>
        /// 给 <see cref="UIElement"/> 挂上 HoverTrigger 的进入/离开订阅，
        /// 并把「进入」回调的退订登记到 UI 的 disposable 列表（随 UI 关闭自动清理）。
        /// </summary>
        public static void AttachHover(UIElement element, HoverTrigger trigger, Action<PointerEventData> onHoverStart)
        {
            if (element == null || trigger == null || onHoverStart == null)
            {
                return;
            }

            trigger.OnHoverStart += onHoverStart;
            // 退订发生在游戏对象销毁路径上，必须 fail-open：异常不得穿透游戏调用链。
            element.AddDisposable(() =>
            {
                try
                {
                    trigger.OnHoverStart -= onHoverStart;
                }
                catch (Exception e)
                {
                    Plugin.Logger?.LogWarning($"[Tooltip] 退订 hover 失败：{e.Message}");
                }
            });

            // 先移除再加，避免重复 Add 造成同一实例多次订阅。
            trigger.OnHoverEnd -= OnHoverEnd;
            trigger.OnHoverEnd += OnHoverEnd;
        }

        /// <summary>
        /// 挂接统一的「悬停 → 写入并显示调试块」路径：<paramref name="debugInfoFactory"/> 返回 null 表示
        /// 本次悬停不显示（空槽判定等）。供物品/任务/槽位各类视图复用，消除重复闭包。
        /// </summary>
        public static void AttachDebugHover(UIElement element, HoverTrigger trigger, Func<DebugInfo> debugInfoFactory)
        {
            AttachHover(element, trigger, eventData => ShowDebugInfo(debugInfoFactory));
        }

        private static void ShowDebugInfo(Func<DebugInfo> debugInfoFactory)
        {
            try
            {
                if (!TooltipSettings.ShowDebugInfoValue)
                {
                    return;
                }

                DebugInfo debugInfo = debugInfoFactory?.Invoke();
                if (debugInfo == null)
                {
                    return;
                }

                DebugTooltipState.SetDebugInfo(debugInfo);
                ShowTooltip(string.Empty);
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 悬停调试信息处理失败：{e.Message}");
            }
        }

        private static void OnHoverEnd(PointerEventData eventData)
        {
            CloseTooltip();
        }

        /// <summary>关闭当前 tooltip（失败静默，绝不穿透游戏调用链）。</summary>
        public static void CloseTooltip()
        {
            try
            {
                var context = ItemUiContext.Instance;
                if (context == null)
                {
                    return;
                }

                var tooltip = context.Tooltip;
                if (tooltip != null)
                {
                    tooltip.Close();
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 关闭 tooltip 失败：{e.Message}");
            }
        }

        /// <summary>在当前 tooltip 上显示给定文本（空串表示仅显示调试块）。</summary>
        /// <remarks>
        /// Mono 下 <c>SimpleTooltip.Show(string text, Vector2? offset = null, float delay = 0f,
        /// float? maxWidth = null)</c> 的可选参数为普通托管 Nullable，传 null 语义等同上游默认值，
        /// 显式写全以便阅读（无 IL2CPP interop NRE 面）。
        /// </remarks>
        public static void ShowTooltip(string text)
        {
            try
            {
                var context = ItemUiContext.Instance;
                if (context == null)
                {
                    return;
                }

                var tooltip = context.Tooltip;
                if (tooltip == null)
                {
                    return;
                }

                tooltip.Show(text, null, 0f, null);
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 显示 tooltip 失败：{e.Message}");
            }
        }

        /// <summary>把三档复制提示追加到调试块。</summary>
        public static void AppendCopyPrompts(StringBuilder sb, DebugInfo debugInfo)
        {
            if (sb == null || debugInfo == null)
            {
                return;
            }

            sb.AppendLine(debugInfo.CopyPrompt);
            if (!string.IsNullOrEmpty(debugInfo.AltCopyPrompt))
            {
                sb.AppendLine(debugInfo.AltCopyPrompt);
            }

            if (!string.IsNullOrEmpty(debugInfo.FullCopyPrompt))
            {
                sb.AppendLine(debugInfo.FullCopyPrompt);
            }
        }
    }
}

using System;
using SamMeow.DebugToolkit.Modules.Tooltip.Patches;

namespace SamMeow.DebugToolkit.Modules.Tooltip
{
    /// <summary>
    /// Tooltip 调试信息模块（移植自 DebugTooltip）注册点。
    /// <see cref="Enable"/> = 绑定配置 + 订阅配置变更 + 启用全部补丁；
    /// <see cref="Disable"/> = 退订 + 撤销全部补丁 + 清理克隆 UI 与残留组件。
    /// </summary>
    /// <remarks>
    /// 上游 DebugTooltip 的 6 组补丁在本模块重新装配：
    /// Tooltip（SimpleTooltip/PriceTooltip）、Item（GridItemView/TradingItemView/SlotView/
    /// ModSlotView/ModdingScreenSlotView）、Quest（QuestListItem/NotesTask/QuestObjectiveView）、
    /// Trader（TraderCard 显式接口进出）、Shortcut（ItemUiContext.Update）。
    /// 补丁生命周期经共享 <see cref="PatchSet"/>：逐项装配 / 逐项撤销，全程 fail-open。
    /// <see cref="Disable"/> 无条件清理（不得因 <c>_enabled == false</c> 早退），
    /// 覆盖「Enable 曾中途失败」的部分装配状态。
    /// 4.1.6 = Mono：<c>SPT.Reflection.Patching.ModulePatch</c> 自带 <c>Disable()</c>，
    /// 撤销直接调用，无需 Harmony.UnpatchID。
    /// </remarks>
    internal static class TooltipModule
    {
        private static readonly PatchSet Patches = new PatchSet("Tooltip");

        private static bool _enabled;

        /// <summary>配置变更处理器的字段化引用，Disable 时据此退订（避免匿名 lambda 无法退订）。</summary>
        private static EventHandler _showDebugInfoChangedHandler;

        public static void Enable()
        {
            if (_enabled)
            {
                return;
            }

            try
            {
                TooltipSettings.Bind(Settings.Config);
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 配置绑定失败，使用默认值：{e.Message}");
            }

            // 关闭调试信息时销毁 PriceTooltip 克隆出来的调试文本。字段化 handler，便于 Disable 退订。
            try
            {
                var showEntry = TooltipSettings.ShowDebugInfo;
                if (showEntry != null)
                {
                    _showDebugInfoChangedHandler = OnShowDebugInfoChanged;
                    showEntry.SettingChanged += _showDebugInfoChangedHandler;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 配置变化订阅失败：{e.Message}");
            }

            Patches.Enable(new TooltipShowPatch());
            Patches.Enable(new PriceTooltipShowPatch());
            Patches.Enable(new GridItemViewPatch());
            Patches.Enable(new TradingItemViewPatch());
            Patches.Enable(new SlotViewPatch());
            Patches.Enable(new ModSlotViewEnterPatch());
            Patches.Enable(new ModSlotArmorPatch());
            Patches.Enable(new ModdingScreenSlotViewPatch());
            Patches.Enable(new QuestListItemPatch());
            Patches.Enable(new NotesTaskPatch());
            Patches.Enable(new QuestObjectivePatch());
            Patches.Enable(new TraderCardEnterPatch());
            Patches.Enable(new TraderCardExitPatch());
            Patches.Enable(new TooltipShortcutPatch());

            _enabled = true;
            Plugin.Logger?.LogInfo($"[Tooltip] 模块已启用（补丁 {Patches.Count} 个，配置节 '{Settings.SectionTooltip}'）。");
        }

        public static void Disable()
        {
            // 无条件清理：即使 Enable 曾中途失败或 _enabled == false，也必须撤销一切已装配项。

            // 退订配置变更（逐项 fail-open，不因未启用而跳过）。
            try
            {
                var showEntry = TooltipSettings.ShowDebugInfo;
                if (showEntry != null && _showDebugInfoChangedHandler != null)
                {
                    showEntry.SettingChanged -= _showDebugInfoChangedHandler;
                }
            }
            catch (Exception e)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] 配置变化退订失败：{e.Message}");
            }
            finally
            {
                _showDebugInfoChangedHandler = null;
            }

            // 销毁 PriceTooltip 克隆的调试文本（置 null + Destroy，fail-open）。
            PriceTooltipShowPatch.DestroyDebugText();

            // 移除挂在 tooltip 上的 TooltipCopier 组件及其持有的临时调试文本（US43「组件无残留」）。
            TooltipCopier.RemoveAll();

            // ModulePatch 自带 Disable()（内部 Unpatch + 从 ModPatchCache 移除）；逐项 fail-open。
            Patches.DisableAll();

            _enabled = false;
            Plugin.Logger?.LogInfo("[Tooltip] 模块已停用，全部补丁已撤销。");
        }

        /// <summary>Show Debug Tooltip 配置变化：关闭时清理 PriceTooltip 克隆文本。fail-open。</summary>
        private static void OnShowDebugInfoChanged(object sender, EventArgs e)
        {
            try
            {
                PriceTooltipShowPatch.OnShowDebugInfoChanged();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[Tooltip] Show Debug Tooltip 变更处理失败（已忽略）：{ex.Message}");
            }
        }
    }
}

using BepInEx.Configuration;
using UnityEngine;

namespace SamMeow.DebugToolkit.Modules.Tooltip
{
    /// <summary>
    /// Tooltip 模块配置面（节 <see cref="Settings.SectionTooltip"/>）。
    /// </summary>
    /// <remarks>
    /// 与上游 DebugTooltip/Settings.cs 等价，差异：
    /// <list type="bullet">
    /// <item>配置经共享的 <see cref="Settings.Config"/> 绑定（统一 F12 面）。</item>
    /// <item>4.1.6 = Mono / BepInEx 5：<c>KeyboardShortcut</c> 由 BepInEx 核心提供（无 IL2CPP 的 CM shim），
    /// 键名英文、显示名中文；本模块不引入列表型配置。</item>
    /// </list>
    /// </remarks>
    internal static class TooltipSettings
    {
        private const string Section = Settings.SectionTooltip;

        public static ConfigEntry<bool> ShowDebugInfo { get; private set; }

        public static ConfigEntry<bool> ShowCopyPrompt { get; private set; }

        public static ConfigEntry<KeyboardShortcut> ToggleShortcut { get; private set; }

        /// <summary>配置是否已成功绑定。</summary>
        public static bool Bound { get; private set; }

        public static void Bind(ConfigFile config)
        {
            if (config == null)
            {
                Bound = false;
                Plugin.Logger?.LogWarning("[Tooltip] ConfigFile 为 null，配置面跳过绑定。");
                return;
            }

            ShowDebugInfo = config.Bind(
                Section,
                "Show Debug Tooltip",
                true,
                ConfigDescriptions.Describe("显示调试信息", "是否在物品/任务/商人 tooltip 中追加调试信息块。"));

            ShowCopyPrompt = config.Bind(
                Section,
                "Show Copy Instructions",
                true,
                ConfigDescriptions.Describe("显示复制提示", "是否在调试块中显示复制提示文案。"));

            ToggleShortcut = config.Bind(
                Section,
                "Toggle Shortcut",
                new KeyboardShortcut(KeyCode.F9),
                ConfigDescriptions.Describe("切换快捷键", "免开 F12 切换 tooltip 调试信息的快捷键（默认 F9）。"));

            Bound = true;
        }

        /// <summary>展示调试块开关；配置缺失时按默认 true 处理。</summary>
        public static bool ShowDebugInfoValue => ShowDebugInfo == null || ShowDebugInfo.Value;

        /// <summary>展示复制提示开关；配置缺失时按默认 true 处理。</summary>
        public static bool ShowCopyPromptValue => ShowCopyPrompt == null || ShowCopyPrompt.Value;
    }
}

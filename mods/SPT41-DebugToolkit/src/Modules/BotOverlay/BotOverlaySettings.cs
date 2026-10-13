using System;
using BepInEx.Configuration;
using UnityEngine;

namespace SamMeow.DebugToolkit.Modules.BotOverlay
{
    /// <summary>
    /// Bot 覆盖层配置面（节 <see cref="Settings.SectionBotOverlay"/>）。
    /// 移植自 BotDebug <c>Helpers/Settings.cs</c>（1.8.0）。
    /// </summary>
    /// <remarks>
    /// 4.1.6 = Mono / BepInEx 5：无 IL2CPP 的 ConfigurationManager 限制，列表型配置用原生下拉
    /// （<see cref="AcceptableValueList{T}"/>，ConfigurationManager 走 <c>DrawComboboxField</c>）；
    /// 数值项保留范围滑块（<see cref="AcceptableValueRange{T}"/>）。
    /// 枚举不满足 <c>AcceptableValueList&lt;T&gt; where T : IEquatable&lt;T&gt;</c>，故 ActiveMode
    /// 以字符串承载 + <see cref="Enum.TryParse"/> 校验（无效值回退 Behaviour）。
    /// </remarks>
    internal static class BotOverlaySettings
    {
        private const string DefaultModeName = nameof(BotInfoMode.Behaviour);

        public static bool Initialized { get; private set; }

        public static ConfigEntry<bool> Enable;
        public static ConfigEntry<string> ActiveMode;
        public static ConfigEntry<KeyboardShortcut> NextModeKey;
        public static ConfigEntry<KeyboardShortcut> PrevModeKey;
        public static ConfigEntry<int> MaxDrawDistance;
        public static ConfigEntry<int> FontSize;

        public static void Init(ConfigFile config)
        {
            if (Initialized)
            {
                return;
            }

            if (config == null)
            {
                Plugin.Logger?.LogWarning("[BotOverlay] ConfigFile 为 null，Bot 覆盖层配置项未绑定。");
                return;
            }

            try
            {
                // 下拉候选来自枚举名（随 STANDALONE 条件编译自动增减），顺序即枚举声明顺序。
                string[] modeNames = Enum.GetNames(typeof(BotInfoMode));

                Enable = config.Bind(
                    Settings.SectionBotOverlay,
                    "Enable",
                    false,
                    ConfigDescriptions.Describe("启用覆盖层", "覆盖层总开关（默认关闭，按需开启）。"));

                ActiveMode = config.Bind(
                    Settings.SectionBotOverlay,
                    "ActiveMode",
                    DefaultModeName,
                    ConfigDescriptions.Describe(
                        "监视模式",
                        "监视模式（原生下拉）。Minimized 为隐藏覆盖层档；无效值自动回退 Behaviour。",
                        new AcceptableValueList<string>(modeNames)));

                NextModeKey = config.Bind(
                    Settings.SectionBotOverlay,
                    "NextModeKey",
                    new KeyboardShortcut(KeyCode.F10),
                    ConfigDescriptions.Describe("下一模式键", "切换到下一个监视模式的按键。"));

                PrevModeKey = config.Bind(
                    Settings.SectionBotOverlay,
                    "PrevModeKey",
                    new KeyboardShortcut(KeyCode.F11),
                    ConfigDescriptions.Describe("上一模式键", "切换到上一个监视模式的按键。"));

                MaxDrawDistance = config.Bind(
                    Settings.SectionBotOverlay,
                    "MaxDrawDistance",
                    1500,
                    ConfigDescriptions.Describe(
                        "最大绘制距离",
                        "绘制 bot 调试框的最大距离。",
                        new AcceptableValueRange<int>(0, 2000)));

                FontSize = config.Bind(
                    Settings.SectionBotOverlay,
                    "FontSize",
                    24,
                    ConfigDescriptions.Describe(
                        "字号",
                        "覆盖层字号。",
                        new AcceptableValueRange<int>(12, 36)));

                Initialized = true;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[BotOverlay] 配置绑定失败：{ex}");
            }
        }

        /// <summary>读取并校验当前模式；无效值回退 <see cref="BotInfoMode.Behaviour"/>。</summary>
        public static BotInfoMode GetActiveMode()
        {
            return TryParseMode(ActiveMode?.Value, out BotInfoMode mode) ? mode : BotInfoMode.Behaviour;
        }

        /// <summary>解析文本模式名（忽略大小写）。无效返回 false 并输出 Behaviour。</summary>
        public static bool TryParseMode(string raw, out BotInfoMode mode)
        {
            mode = BotInfoMode.Behaviour;
            if (string.IsNullOrWhiteSpace(raw))
            {
                return false;
            }

            if (!Enum.TryParse(raw.Trim(), true, out mode))
            {
                mode = BotInfoMode.Behaviour;
                return false;
            }

            if (!Enum.IsDefined(typeof(BotInfoMode), mode))
            {
                mode = BotInfoMode.Behaviour;
                return false;
            }

            return true;
        }

        /// <summary>回写模式（热键循环用）；写入文本与枚举名一致。</summary>
        public static void SetActiveMode(BotInfoMode mode)
        {
            if (ActiveMode != null)
            {
                ActiveMode.Value = mode.ToString();
            }
        }
    }
}

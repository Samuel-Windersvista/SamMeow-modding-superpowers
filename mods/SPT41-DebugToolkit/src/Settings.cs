using BepInEx.Configuration;

namespace SamMeow.DebugToolkit
{
    /// <summary>
    /// 统一 F12 配置面骨架。6 个中文节名常量与模块一一对应；
    /// 工单 01 只绑定 <see cref="SectionGeneral"/> 下的只读版本项，其余节在后续工单填充。
    /// </summary>
    /// <remarks>
    /// 4.1.6 = Mono / BepInEx 5：无 IL2CPP 的 ConfigurationManager 限制，
    /// 列表型配置可后续用原生下拉（AcceptableValueList）；本工单不涉及。
    /// </remarks>
    internal static class Settings
    {
        public const string SectionGeneral = "1. 通用";
        public const string SectionTooltip = "2. 提示信息";
        public const string SectionBotOverlay = "3. Bot 覆盖层";
        public const string SectionInvincibility = "4. 无敌";
        public const string SectionRegeneration = "5. 恢复";
        public const string SectionQoL = "6. 便利";

        /// <summary>已绑定的 ConfigFile（各模块经此绑定自身配置项）。</summary>
        public static ConfigFile Config { get; private set; }

        public static void Init(ConfigFile config)
        {
            if (config == null)
            {
                Plugin.Logger?.LogWarning("[Settings] ConfigFile 为 null，配置面跳过绑定。");
                return;
            }

            Config = config;

            // 键名 "Plugin Version" 保持英文（cfg 兼容）；显示名走 ConfigurationManagerAttributes 机制。
            config.Bind(
                SectionGeneral,
                "Plugin Version",
                Plugin.Version,
                ConfigDescriptions.Describe(
                    "插件版本（只读）",
                    "只读信息：SamMeow.DebugToolkit 版本。",
                    readOnly: true));

            Plugin.Logger?.LogInfo(
                $"[Settings] 已初始化配置面骨架（节：{SectionGeneral} / {SectionTooltip} / " +
                $"{SectionBotOverlay} / {SectionInvincibility} / {SectionRegeneration} / {SectionQoL}）。");
        }
    }
}

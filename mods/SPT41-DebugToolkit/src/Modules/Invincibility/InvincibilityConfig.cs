using System;
using BepInEx.Configuration;

namespace SamMeow.DebugToolkit.Modules.Invincibility
{
    /// <summary>
    /// 无敌模块配置面（节 <see cref="Settings.SectionInvincibility"/> 8 键 +
    /// <see cref="Settings.SectionRegeneration"/> 4 键）。移植 DadGamerMode 1.9.3。
    /// </summary>
    /// <remarks>
    /// 4.1.6 = Mono / BepInEx 5：无 IL2CPP 的 ConfigurationManager 限制，列表型配置
    /// （<c>Keep 1 Health Selection</c>）用原生下拉 <see cref="AcceptableValueList{T}"/>
    /// （ConfigurationManager 走 ComboBox 绘制）；数值项保留范围滑块
    /// （<see cref="AcceptableValueRange{T}"/>）。键名英文、显示名中文，默认全关。
    /// </remarks>
    internal static class InvincibilityConfig
    {
        internal const string SelectionAll = "All";
        internal const string SelectionHeadAndThorax = "Head And Thorax";

        /// <summary><c>Keep 1 Health Selection</c> 的解析后表示（文本配置读取侧校验）。</summary>
        internal enum SelectionMode
        {
            All,
            HeadAndThorax
        }

        /// <summary>配置面是否已成功绑定；未就绪时所有开关视为关闭（零副作用）。</summary>
        internal static bool Ready { get; private set; }

        // 4. 无敌
        internal static ConfigEntry<bool> GodMode { get; private set; }
        internal static ConfigEntry<bool> Keep1Health { get; private set; }
        internal static ConfigEntry<string> Keep1HealthSelection { get; private set; }
        internal static ConfigEntry<bool> IgnoreHeadshotDamage { get; private set; }
        internal static ConfigEntry<bool> PercentageHeadshotDamage { get; private set; }
        internal static ConfigEntry<int> PercentDamageOnlyHead { get; private set; }
        internal static ConfigEntry<int> PercentDamageAllBody { get; private set; }
        internal static ConfigEntry<bool> NoFallingDamage { get; private set; }

        // 5. 恢复
        internal static ConfigEntry<bool> CODMode { get; private set; }
        internal static ConfigEntry<float> HealRate { get; private set; }
        internal static ConfigEntry<float> HealWait { get; private set; }
        internal static ConfigEntry<bool> BleedingDamage { get; private set; }

        internal static void Init()
        {
            if (Ready)
            {
                return;
            }

            ConfigFile config = Settings.Config;
            if (config == null)
            {
                Plugin.Logger?.LogWarning(
                    "[Invincibility] Settings.Config 为 null，配置面未绑定（所有开关保持关闭）。");
                return;
            }

            try
            {
                const string invincibility = Settings.SectionInvincibility;
                const string regeneration = Settings.SectionRegeneration;

                GodMode = config.Bind(
                    invincibility, "God Mode", false,
                    ConfigDescriptions.Describe("无敌模式", "除坠落伤害外的全部伤害清零。"));

                Keep1Health = config.Bind(
                    invincibility, "Keep 1 Health", false,
                    ConfigDescriptions.Describe("保留 1 点血", "选中部位在致死伤害下不再掉血（部位摧毁同步被抑制）。"));

                Keep1HealthSelection = config.Bind(
                    invincibility, "Keep 1 Health Selection", SelectionAll,
                    ConfigDescriptions.Describe(
                        "保护范围",
                        "Keep 1 Health 作用范围（原生下拉）。",
                        new AcceptableValueList<string>(SelectionAll, SelectionHeadAndThorax)));

                IgnoreHeadshotDamage = config.Bind(
                    invincibility, "Ignore Headshot Damage", false,
                    ConfigDescriptions.Describe("忽略爆头伤害", "命中头部时伤害清零。"));

                PercentageHeadshotDamage = config.Bind(
                    invincibility, "Percentage Headshot Damage", false,
                    ConfigDescriptions.Describe("头部百分比减伤", "启用独立的头部百分比减伤（作用于全身百分比之后）。"));

                PercentDamageOnlyHead = config.Bind(
                    invincibility, "% Damage Received (Only Head)", 100,
                    ConfigDescriptions.Describe(
                        "头部伤害百分比",
                        "头部受到伤害的百分比（需 Percentage Headshot Damage 开启）。",
                        new AcceptableValueRange<int>(0, 100)));

                PercentDamageAllBody = config.Bind(
                    invincibility, "% Damage Received (All Body)", 100,
                    ConfigDescriptions.Describe(
                        "全身伤害百分比",
                        "全身受到伤害的百分比（100 为正常）。",
                        new AcceptableValueRange<int>(0, 100)));

                NoFallingDamage = config.Bind(
                    invincibility, "No Falling Damage", false,
                    ConfigDescriptions.Describe("无坠落伤害", "把 FallSafeHeight 拉满，免除坠落伤害。"));

                CODMode = config.Bind(
                    regeneration, "CODMode", false,
                    ConfigDescriptions.Describe("自动治疗（CODMode）", "受击后延迟自动治疗（含流血 / 骨折 / 疼痛清理）。"));

                HealRate = config.Bind(
                    regeneration, "CODMode Heal Rate", 10f,
                    ConfigDescriptions.Describe(
                        "治疗速率",
                        "每个治疗节拍为每个可治疗部位恢复的血量。",
                        new AcceptableValueRange<float>(0f, 100f)));

                HealWait = config.Bind(
                    regeneration, "CODMode Heal Wait", 10f,
                    ConfigDescriptions.Describe(
                        "治疗等待（秒）",
                        "上次受击后需保持无伤多久（秒）才开始治疗。",
                        new AcceptableValueRange<float>(0f, 600f)));

                BleedingDamage = config.Bind(
                    regeneration, "Bleeding Damage", false,
                    ConfigDescriptions.Describe("保留流血/骨折", "开启后保留流血 / 骨折等负面效果（不清理）。"));

                Ready = true;
            }
            catch (Exception ex)
            {
                Ready = false;
                Plugin.Logger?.LogError($"[Invincibility] 配置绑定失败：{ex}");
            }
        }

        /// <summary>
        /// 返回经校验的 <c>Keep 1 Health Selection</c>。非法 cfg 值（手改）回退
        /// <see cref="SelectionMode.All"/>。
        /// </summary>
        internal static SelectionMode NormalizedSelection()
        {
            string value = Keep1HealthSelection?.Value;
            return value == SelectionHeadAndThorax ? SelectionMode.HeadAndThorax : SelectionMode.All;
        }
    }
}

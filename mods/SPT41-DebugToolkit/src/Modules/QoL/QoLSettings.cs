using System;
using BepInEx.Configuration;

namespace SamMeow.DebugToolkit.Modules.QoL
{
    /// <summary>
    /// 便利组 QOL 配置面（节 <see cref="Settings.SectionQoL"/>，10 键）。移植 DadGamerMode 1.9.3。
    /// </summary>
    /// <remarks>
    /// 4.1.6 = Mono / BepInEx 5：数值项保留范围滑块（<see cref="AcceptableValueRange{T}"/>）。
    /// 键名英文、显示名中文，默认全关（数值项取上游默认）。负重键 <c>Item Total Weight %</c>
    /// 为「物品总重百分比」语义（100 = 正常）。
    /// </remarks>
    internal static class QoLSettings
    {
        /// <summary>配置面是否已成功绑定；未就绪时所有开关视为关闭（零副作用）。</summary>
        internal static bool Ready { get; private set; }

        internal static ConfigEntry<bool> InfiniteStamina { get; private set; }
        internal static ConfigEntry<bool> InfiniteEnergy { get; private set; }
        internal static ConfigEntry<bool> InfiniteHydration { get; private set; }
        internal static ConfigEntry<bool> InstantProduction { get; private set; }
        internal static ConfigEntry<bool> InstantConstruction { get; private set; }
        internal static ConfigEntry<bool> ReloadUnloadToggle { get; private set; }
        internal static ConfigEntry<float> ReloadMagSpeed { get; private set; }
        internal static ConfigEntry<float> UnloadMagSpeed { get; private set; }
        internal static ConfigEntry<float> EnemyDamageMultiplier { get; private set; }
        internal static ConfigEntry<int> ItemTotalWeight { get; private set; }

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
                    "[QoL] Settings.Config 为 null，配置面未绑定（所有开关保持关闭）。");
                return;
            }

            try
            {
                const string qol = Settings.SectionQoL;

                InfiniteStamina = config.Bind(
                    qol, "Infinite Stamina", false,
                    ConfigDescriptions.Describe("无限体力", "身体体力 / 手部体力 / 氧气始终维持满值。"));

                InfiniteEnergy = config.Bind(
                    qol, "Infinite Energy", false,
                    ConfigDescriptions.Describe("无限能量", "能量始终维持满值（无需进食）。"));

                InfiniteHydration = config.Bind(
                    qol, "Infinite Hydration", false,
                    ConfigDescriptions.Describe("无限水分", "水分始终维持满值（无需饮水）。"));

                InstantProduction = config.Bind(
                    qol, "Instant Production", false,
                    ConfigDescriptions.Describe("瞬间生产", "藏身处制造即刻完成（比特币农场除外）。"));

                InstantConstruction = config.Bind(
                    qol, "Instant Construction", false,
                    ConfigDescriptions.Describe("瞬间建造", "藏身处建造 / 升级计时即刻完成。"));

                ReloadUnloadToggle = config.Bind(
                    qol, "Enable Reload/Unload Mag Speed", false,
                    ConfigDescriptions.Describe("启用换弹/卸弹速度", "启用换弹 / 卸弹速度倍率的改写。"));

                ReloadMagSpeed = config.Bind(
                    qol, "Reload Mag Speed", 0.85f,
                    ConfigDescriptions.Describe(
                        "换弹速度倍率",
                        "弹匣装填速度倍率（越小越快）。",
                        new AcceptableValueRange<float>(0f, 0.85f)));

                UnloadMagSpeed = config.Bind(
                    qol, "Unload Mag Speed", 0.3f,
                    ConfigDescriptions.Describe(
                        "卸弹速度倍率",
                        "弹匣卸弹速度倍率（越小越快）。",
                        new AcceptableValueRange<float>(0f, 0.3f)));

                EnemyDamageMultiplier = config.Bind(
                    qol, "Enemy Damage Multiplier", 1f,
                    ConfigDescriptions.Describe(
                        "对敌伤害倍率",
                        "对非本玩家（敌人 / 其他 bot）造成的伤害倍率（默认 1）。",
                        new AcceptableValueRange<float>(1f, 20f)));

                ItemTotalWeight = config.Bind(
                    qol, "Item Total Weight %", 100,
                    ConfigDescriptions.Describe(
                        "物品总重百分比",
                        "物品总重百分比（100 为正常，越小负重越轻；须战局前设置）。",
                        new AcceptableValueRange<int>(0, 100)));

                Ready = true;
            }
            catch (Exception ex)
            {
                Ready = false;
                Plugin.Logger?.LogError($"[QoL] 配置绑定失败：{ex}");
            }
        }
    }
}

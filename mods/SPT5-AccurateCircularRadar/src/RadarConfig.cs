using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Every BepInEx setting the radar exposes, grouped by the section it appears under in the F12 menu.
    /// </summary>
    /// <remarks>
    /// Section names and key strings are the on-disk config format. Changing one silently resets that
    /// setting for every existing user, so they are kept verbatim - only the C# identifiers are ours.
    /// Keys are looked up through <see cref="Locales"/>, which means <see cref="Language"/> has to be
    /// bound before anything else.
    /// </remarks>
    internal static class RadarConfig
    {
        // 中文本地化（本移植）：F12 界面分区名直接使用中文。
        private const string BaseSection = "基础设置";
        private const string AdvancedSection = "高级设置";
        private const string ColorSection = "颜色设置";
        private const string UiSection = "界面设置";

        // Base
        public static ConfigEntry<string> Language { get; private set; } = null!;
        public static ConfigEntry<bool> Enabled { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleRadarKey { get; private set; } = null!;
        public static ConfigEntry<bool> PulseEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> FireModeEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> CompassEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> MinefieldEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> ExfilEnabled { get; private set; } = null!;

        // Advanced
        public static ConfigEntry<bool> CorpseEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> CorpseTypeEnabled { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleCorpseKey { get; private set; } = null!;
        public static ConfigEntry<bool> LootEnabled { get; private set; } = null!;
        public static ConfigEntry<bool> WishlistLootEnabled { get; private set; } = null!;
        public static ConfigEntry<KeyboardShortcut> ToggleLootKey { get; private set; } = null!;
        public static ConfigEntry<bool> LootValuePerSlot { get; private set; } = null!;

        // UI
        public static ConfigEntry<float> HudSize { get; private set; } = null!;
        public static ConfigEntry<float> BlipSize { get; private set; } = null!;
        public static ConfigEntry<float> DistanceScale { get; private set; } = null!;
        public static ConfigEntry<float> HeightThreshold { get; private set; } = null!;
        public static ConfigEntry<int> OffsetX { get; private set; } = null!;
        public static ConfigEntry<int> OffsetY { get; private set; } = null!;
        public static ConfigEntry<int> OuterRange { get; private set; } = null!;
        public static ConfigEntry<int> InnerRange { get; private set; } = null!;
        public static ConfigEntry<float> ScanInterval { get; private set; } = null!;
        public static ConfigEntry<int> LootThreshold { get; private set; } = null!;

        // Colors
        public static ConfigEntry<Color> BossColor { get; private set; } = null!;
        public static ConfigEntry<Color> ScavColor { get; private set; } = null!;
        public static ConfigEntry<Color> UsecColor { get; private set; } = null!;
        public static ConfigEntry<Color> BearColor { get; private set; } = null!;
        public static ConfigEntry<Color> LootColor { get; private set; } = null!;
        public static ConfigEntry<Color> WishlistLootColor { get; private set; } = null!;
        public static ConfigEntry<Color> CorpseColor { get; private set; } = null!;
        public static ConfigEntry<Color> BackgroundColor { get; private set; } = null!;
        public static ConfigEntry<Color> MinefieldColor { get; private set; } = null!;

        public static void Bind(ConfigFile config)
        {
            // 中文本地化（本移植）：默认 ZH；键名/描述经 Locales 解析。
            // [CRITICAL] 不使用 AcceptableValueList —— 5.0 的 ConfigurationManager 用 ComboBox 渲染
            // 下拉列表，而 ComboBox 依赖被 IL2CPP 剥离的 GUI.DoButtonGrid，会在 F12 中每帧抛
            // NotSupportedException 并破坏 GUIClip 栈（实战：6,477 条刷屏）。改为纯文本输入，
            // 非法值由 Locales 回退 EN。
            // [CRITICAL] 键名随语言变化（见 MigrateLanguageKeys）：改动 Language 后需重启游戏生效。
            Language = config.Bind(BaseSection, "Language", "ZH",
                new ConfigDescription("界面语言 / UI language (EN / ZH / RU / KO)；改动后需重启游戏生效 / restart required"));

            Enabled = config.Bind(BaseSection, Key("radar_enable"), true);
            ToggleRadarKey = config.Bind(BaseSection, Key("radar_enable_shortcut"), new KeyboardShortcut(KeyCode.F10));
            PulseEnabled = config.Bind(BaseSection, Key("radar_pulse_enable"), true, Key("radar_pulse_enable_info"));
            FireModeEnabled = config.Bind(BaseSection, Key("radar_fire_mode_enable"), false, Key("radar_fire_mode_enable_info"));
            CompassEnabled = config.Bind(BaseSection, Key("radar_compass_enable"), false, Key("radar_compass_enable_info"));
            MinefieldEnabled = config.Bind(BaseSection, Key("radar_minefield_enable"), false, Key("radar_minefield_enable_info"));
            ExfilEnabled = config.Bind(BaseSection, Key("radar_exfil_enable"), false, Key("radar_exfil_enable_info"));

            CorpseEnabled = config.Bind(AdvancedSection, Key("radar_corpse_enable"), false);
            CorpseTypeEnabled = config.Bind(AdvancedSection, Key("radar_corpse_type_enable"), false);
            ToggleCorpseKey = config.Bind(AdvancedSection, Key("radar_corpse_shortcut"), new KeyboardShortcut(KeyCode.F11));
            LootEnabled = config.Bind(AdvancedSection, Key("radar_loot_enable"), false);
            WishlistLootEnabled = config.Bind(AdvancedSection, Key("radar_wishlist_enable"), false);
            ToggleLootKey = config.Bind(AdvancedSection, Key("radar_loot_shortcut"), new KeyboardShortcut(KeyCode.F9));
            LootValuePerSlot = config.Bind(AdvancedSection, Key("radar_loot_per_slot"), false);

            HudSize = config.Bind(UiSection, Key("radar_hud_size"), 0.8f,
                new ConfigDescription(Key("radar_hud_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            BlipSize = config.Bind(UiSection, Key("radar_blip_size"), 0.7f,
                new ConfigDescription(Key("radar_blip_size_info"), new AcceptableValueRange<float>(0.0f, 1f)));
            DistanceScale = config.Bind(UiSection, Key("radar_distance_scale"), 0.7f,
                new ConfigDescription(Key("radar_distance_scale_info"), new AcceptableValueRange<float>(0.1f, 2f)));
            HeightThreshold = config.Bind(UiSection, Key("radar_y_height_threshold"), 1f,
                new ConfigDescription(Key("radar_y_height_threshold_info"), new AcceptableValueRange<float>(1f, 4f)));
            OffsetX = config.Bind(UiSection, Key("radar_x_position"), 300,
                new ConfigDescription(Key("radar_x_position_info"), new AcceptableValueRange<int>(0, 4000)));
            OffsetY = config.Bind(UiSection, Key("radar_y_position"), 150,
                new ConfigDescription(Key("radar_y_position_info"), new AcceptableValueRange<int>(0, 3000)));
            OuterRange = config.Bind(UiSection, Key("radar_outer_range"), 128,
                new ConfigDescription(Key("radar_outer_range_info"), new AcceptableValueRange<int>(32, 1024)));
            InnerRange = config.Bind(UiSection, Key("radar_inner_range"), 0,
                new ConfigDescription(Key("radar_inner_range_info"), new AcceptableValueRange<int>(0, 64)));
            ScanInterval = config.Bind(UiSection, Key("radar_scan_interval"), 1f,
                new ConfigDescription(Key("radar_scan_interval_info"), new AcceptableValueRange<float>(0.1f, 30f)));
            LootThreshold = config.Bind(UiSection, Key("radar_loot_threshold"), 30000,
                new ConfigDescription(Key("radar_loot_threshold_info"), new AcceptableValueRange<int>(5000, 200000)));

            BossColor = config.Bind(ColorSection, Key("radar_boss_blip_color"), new Color(1f, 0f, 0f));
            ScavColor = config.Bind(ColorSection, Key("radar_scav_blip_color"), new Color(0f, 1f, 0f));
            UsecColor = config.Bind(ColorSection, Key("radar_usec_blip_color"), new Color(1f, 1f, 0f));
            BearColor = config.Bind(ColorSection, Key("radar_bear_blip_color"), new Color(1f, 0.5f, 0f));
            LootColor = config.Bind(ColorSection, Key("radar_loot_blip_color"), new Color(0.9f, 0.5f, 0.5f));
            WishlistLootColor = config.Bind(ColorSection, Key("radar_wishlist_loot_blip_color"), new Color(0.9f, 0.8f, 0.5f));
            CorpseColor = config.Bind(ColorSection, Key("radar_corpse_blip_color"), new Color(0.5f, 0.5f, 0.5f));
            BackgroundColor = config.Bind(ColorSection, Key("radar_background_color"), new Color(0f, 0.7f, 0.85f));
            MinefieldColor = config.Bind(ColorSection, Key("radar_minefield_color"), new Color(0.7f, 0.7f, 0.7f, 0.3f));

            // 语言键集迁移（方案 B：保留 Language 切换，重启生效，设置值自动搬迁）。
            RegisterMigrators();
            MigrateLanguageKeys(config);
        }

        private static string Key(string id) => Locales.GetTranslatedString(id);

        // --- 语言键集迁移 -----------------------------------------------------------------------

        /// <summary>
        /// 「上次绑定键集的语言」标记文件（与配置文件同目录）。
        /// 语言键名在加载时一次性确定，语言一变就生成全新键集、旧键成为孤儿（值分裂），
        /// 故需要跨启动记录上次绑定的语言，才能在本次启动把旧键的值搬到新键。
        /// </summary>
        private const string LanguageMarkerFileName = "com.leonana69.radar.lang";

        /// <summary>设置 id -> 「把旧语言键集的值搬到当前条目」的动作（<see cref="RegisterMigrators"/> 填充）。</summary>
        private static readonly List<Migration> Migrators = new List<Migration>();

        private sealed class Migration
        {
            public string Id { get; }
            public Action<ConfigFile, string> Apply { get; }

            public Migration(string id, Action<ConfigFile, string> apply)
            {
                Id = id;
                Apply = apply;
            }
        }

        /// <summary>
        /// 登记一个设置条目的迁移动作。用泛型闭包而非反射：<c>ConfigEntry&lt;T&gt;.Value</c> 的类型
        /// 由编译器确定，避免在 IL2CPP 运行时对托管泛型做 MakeGenericMethod。
        /// </summary>
        private static void Track<T>(string id, ConfigEntry<T> entry)
        {
            Migrators.Add(new Migration(id, (oldFile, oldLanguage) =>
            {
                // 键不存在时 Bind 读回传入的默认值（= 当前值），赋值等价 no-op，安全。
                ConfigEntry<T> oldEntry = oldFile.Bind(
                    entry.Definition.Section,
                    Locales.GetTranslatedString(id, oldLanguage),
                    entry.Value);

                entry.Value = oldEntry.Value;
            }));
        }

        /// <summary>登记全部 33 个设置的迁移动作（<see cref="Language"/> 自身不迁移）。</summary>
        private static void RegisterMigrators()
        {
            Migrators.Clear();

            // 基础设置
            Track("radar_enable", Enabled);
            Track("radar_enable_shortcut", ToggleRadarKey);
            Track("radar_pulse_enable", PulseEnabled);
            Track("radar_fire_mode_enable", FireModeEnabled);
            Track("radar_compass_enable", CompassEnabled);
            Track("radar_minefield_enable", MinefieldEnabled);
            Track("radar_exfil_enable", ExfilEnabled);

            // 高级设置
            Track("radar_corpse_enable", CorpseEnabled);
            Track("radar_corpse_type_enable", CorpseTypeEnabled);
            Track("radar_corpse_shortcut", ToggleCorpseKey);
            Track("radar_loot_enable", LootEnabled);
            Track("radar_wishlist_enable", WishlistLootEnabled);
            Track("radar_loot_shortcut", ToggleLootKey);
            Track("radar_loot_per_slot", LootValuePerSlot);

            // 界面设置
            Track("radar_hud_size", HudSize);
            Track("radar_blip_size", BlipSize);
            Track("radar_distance_scale", DistanceScale);
            Track("radar_y_height_threshold", HeightThreshold);
            Track("radar_x_position", OffsetX);
            Track("radar_y_position", OffsetY);
            Track("radar_outer_range", OuterRange);
            Track("radar_inner_range", InnerRange);
            Track("radar_scan_interval", ScanInterval);
            Track("radar_loot_threshold", LootThreshold);

            // 颜色设置
            Track("radar_boss_blip_color", BossColor);
            Track("radar_scav_blip_color", ScavColor);
            Track("radar_usec_blip_color", UsecColor);
            Track("radar_bear_blip_color", BearColor);
            Track("radar_loot_blip_color", LootColor);
            Track("radar_wishlist_loot_blip_color", WishlistLootColor);
            Track("radar_corpse_blip_color", CorpseColor);
            Track("radar_background_color", BackgroundColor);
            Track("radar_minefield_color", MinefieldColor);
        }

        /// <summary>
        /// 语言切换后的设置值迁移（方案 B）。
        /// </summary>
        /// <remarks>
        /// 流程：读标记 → 得到「上次绑定键集的语言」；与本次有效语言不同时，用**独立 ConfigFile**
        /// （不挂到插件，故不污染 F12 菜单；<c>saveOnInit:false</c> 保证只读不写盘）按旧语言键名
        /// 逐个读回旧值，赋给当前已绑定的条目，然后 <c>config.Save()</c> 持久化。
        /// 标记缺失 / 内容损坏 → 无从判断旧键集，跳过迁移（只补写标记）；整体 try/catch，
        /// 失败只记 Warning，绝不中断插件加载。
        /// </remarks>
        private static void MigrateLanguageKeys(ConfigFile config)
        {
            string currentLanguage = Locales.ResolveLanguage(Language.Value);
            string markerPath = LanguageMarkerPath(config);

            string lastLanguage = null;
            try
            {
                if (File.Exists(markerPath))
                    lastLanguage = (File.ReadAllText(markerPath) ?? string.Empty).Trim();
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Language marker unreadable ({markerPath}): {e.Message}");
            }

            // 首次运行 / 标记缺失 / 标记损坏：不猜旧键集，直接补写标记。
            if (!Locales.IsSupportedLanguage(lastLanguage))
            {
                WriteLanguageMarker(markerPath, currentLanguage);
                return;
            }

            if (Locales.ResolveLanguage(lastLanguage) == currentLanguage)
            {
                WriteLanguageMarker(markerPath, currentLanguage);
                return;
            }

            try
            {
                // 独立 ConfigFile：构造时一次性读入文件内容，之后按旧语言键名取值不再碰磁盘。
                var oldFile = new ConfigFile(config.ConfigFilePath, false)
                {
                    // 只读用途：任何情况下都不让这个句柄写盘。
                    SaveOnConfigSet = false,
                };

                // 迁移期间关掉「赋值即写盘」：否则 33 次赋值 = 33 次整文件写入（且中途落盘的状态不完整）。
                bool saveOnConfigSet = config.SaveOnConfigSet;
                config.SaveOnConfigSet = false;
                try
                {
                    foreach (Migration migration in Migrators)
                        migration.Apply(oldFile, lastLanguage);

                    config.Save();
                }
                finally
                {
                    config.SaveOnConfigSet = saveOnConfigSet;
                }

                RadarPlugin.Log.LogInfo(
                    $"Language key set migrated: {lastLanguage} -> {currentLanguage} ({Migrators.Count} settings).");
            }
            catch (Exception e)
            {
                // 迁移失败不更新标记：旧值仍在文件里，下次启动可重试（避免静默丢值）。
                RadarPlugin.Log.LogWarning(
                    $"Language key migration failed ({lastLanguage} -> {currentLanguage}): {e.Message}");
                return;
            }

            WriteLanguageMarker(markerPath, currentLanguage);
        }

        /// <summary>标记文件路径 = 配置文件同目录 + <see cref="LanguageMarkerFileName"/>。</summary>
        private static string LanguageMarkerPath(ConfigFile config)
        {
            string directory = Path.GetDirectoryName(config.ConfigFilePath);
            return Path.Combine(directory ?? string.Empty, LanguageMarkerFileName);
        }

        /// <summary>写标记文件；任何 I/O 失败都静默降级为 Warning（不影响加载）。</summary>
        private static void WriteLanguageMarker(string path, string language)
        {
            try
            {
                File.WriteAllText(path, language);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogWarning($"Could not write language marker ({path}): {e.Message}");
            }
        }
    }
}

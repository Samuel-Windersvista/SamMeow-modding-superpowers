using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using DoorBreach.Models;

namespace DoorBreach
{
    /// <summary>
    /// 统一 F12 配置面：以本地 BepInEx 配置承载全部限制旋钮（见 ADR-0002）。
    /// 7 个中文节名；节 4 为后续工单（04）预留，节 3（破坏）与节 7（调试）由工单 03 填充。
    /// </summary>
    /// <remarks>
    /// 列表型配置为逗号分隔字符串（键名英文，显示名中文，范围项走 AcceptableValueRange 滑块）。
    /// </remarks>
    internal static class Settings
    {
        public const string SectionGeneral = "1. 通用";
        public const string SectionBreach = "2. 破门";
        public const string SectionDestruction = "3. 破坏";     // 预留（工单 03/05 填充）
        public const string SectionInteractions = "4. 交互";    // 预留（工单 04/05 填充）
        public const string SectionExplosive = "5. 爆炸";
        public const string SectionExclusions = "6. 排除与开关";
        public const string SectionDebug = "7. 调试";           // 预留（工单 05/07 填充）

        // 默认武器/弹药/房间 ID 串（源自上游 additional_files 与配方，去掉重复项）。
        private const string DefaultShotgunWeapons =
            "5580223e4bdc2d1c128b457f,54491c4f4bdc2db1078b4568,6259b864ebedf17603599e88," +
            "5e870397991fd70db46995c8,576165642459773c7a400233,5447b6094bdc2dc3278b4567," +
            "60db29ce99594040e04c4a27,56dee2bdd2720bc8328b4567,606dae0ab0e443224b421bb7," +
            "64748cb8de82c85eaf0a273a,5a7828548dc32e5a9c28b516";
        private const string DefaultGrenadeLauncherWeapons =
            "62e7e7bbe6da9612f743f1e0,6357c98711fb55120211f7e1,5e81ebcd8e146c7080625e15," +
            "5d52cc5ba4b9367408500062,6275303a9f372d6ea97f9ec7,676bf44c5539167c3603e869";
        private const string DefaultMeleeWeapons =
            "63a0b208f444d32d6f03ea1e,6087e570b998180e9f76dc24,5c07df7f0db834001b73588a," +
            "5bffe7930db834001b734a39,57cd379a24597778e7682ecf";
        private const string DefaultOtherWeapons = "";
        private const string DefaultWhitelistedRounds =
            "5d6e68a8a4b9360b6c0d54e2,660137d8481cc6907a0c5cda,660137ef76c1b56143052be8";
        private const string DefaultBlacklistedRooms = "";

        private static ConfigEntry<string> _pluginVersion;
        private static ConfigEntry<bool> _plebMode;
        private static ConfigEntry<bool> _semiPlebMode;
        private static ConfigEntry<bool> _breachingRoundsOpenMetalDoors;
        private static ConfigEntry<bool> _openLootableContainers;
        private static ConfigEntry<bool> _openCarDoors;
        private static ConfigEntry<int> _minHitPoints;
        private static ConfigEntry<int> _maxHitPoints;
        private static ConfigEntry<string> _shotgunWeapons;
        private static ConfigEntry<string> _grenadeLauncherWeapons;
        private static ConfigEntry<string> _meleeWeapons;
        private static ConfigEntry<string> _otherWeapons;
        private static ConfigEntry<string> _whitelistedRounds;
        private static ConfigEntry<int> _explosiveTimerSeconds;
        private static ConfigEntry<bool> _explosionDoesDamage;
        private static ConfigEntry<int> _explosionRadius;
        private static ConfigEntry<int> _explosionDamage;
        private static ConfigEntry<string> _blacklistedRooms;

        // 3. 破坏：DO 破坏系统实时代码按名直读，故须为 public static（不可用私有 _ 命名）。
        public static ConfigEntry<bool> DestructionEnabled;
        public static ConfigEntry<bool> ShowHoles;
        public static ConfigEntry<bool> HingeCollapse;
        public static ConfigEntry<bool> LeafBlocksPlayers;
        public static ConfigEntry<float> HoleSize;
        public static ConfigEntry<float> ThroughPenWoodThin;
        public static ConfigEntry<float> ThroughPenWoodThick;
        public static ConfigEntry<float> ThroughPenMetalThin;
        public static ConfigEntry<float> CellSizeCm;

        // 7. 调试：同上，DO 调试 GUI 实时读值。
        public static ConfigEntry<bool> ShowImpacts;
        public static ConfigEntry<bool> ShowDoorInfo;
        public static ConfigEntry<bool> ShowDamageMap;

        /// <summary>已绑定的 ConfigFile。</summary>
        public static ConfigFile Config { get; private set; }

        /// <summary>绑定全部配置项；Config 为 null 时记警告并跳过。</summary>
        public static void Init(ConfigFile config)
        {
            if (config == null)
            {
                DoorBreachPlugin.Logger?.LogWarning("[Settings] ConfigFile 为 null，配置面跳过绑定。");
                return;
            }

            Config = config;

            // 1. 通用
            _pluginVersion = config.Bind(
                SectionGeneral,
                "Plugin Version",
                DoorBreachPlugin.PluginVersion,
                ConfigDescriptions.Describe(
                    "插件版本（只读）",
                    "只读信息：Door Breach Overhaul 版本。",
                    readOnly: true));

            // 2. 破门
            _plebMode = config.Bind(
                SectionBreach,
                "Pleb Mode",
                false,
                ConfigDescriptions.Describe(
                    "全放行模式（任何武器均可破门）",
                    "开启后任何伤害来源都可对门/容器/后备箱生效，忽略白名单与材质规则。"));
            _semiPlebMode = config.Bind(
                SectionBreach,
                "Semi Pleb Mode",
                false,
                ConfigDescriptions.Describe(
                    "半放行模式（非金属门任意武器可破）",
                    "开启后非金属材质的门允许任意武器/弹药击破，金属门仍按白名单与材质规则。"));
            _breachingRoundsOpenMetalDoors = config.Bind(
                SectionBreach,
                "Breaching Rounds Open Metal Doors",
                false,
                ConfigDescriptions.Describe(
                    "破门弹可开金属门",
                    "允许破门弹（breaching slug）对金属材质门生效（默认被材质规则挡下）。"));
            _openLootableContainers = config.Bind(
                SectionBreach,
                "Open Lootable Containers",
                true,
                ConfigDescriptions.Describe(
                    "容器可被射开",
                    "允许对 LootableContainer 累积命中并开启（本 fork 默认开，偏离上游默认关）。"));
            _openCarDoors = config.Bind(
                SectionBreach,
                "Open Car Doors",
                true,
                ConfigDescriptions.Describe(
                    "后备箱可被射开",
                    "允许对车辆后备箱（Trunk）累积命中并开启（本 fork 默认开，偏离上游默认关）。"));
            _minHitPoints = config.Bind(
                SectionBreach,
                "Min Hit Points",
                100,
                ConfigDescriptions.Describe(
                    "门锁耐久下限",
                    "铺场时为每个目标随机分配耐久值的下限。",
                    new AcceptableValueRange<int>(1, 100000)));
            _maxHitPoints = config.Bind(
                SectionBreach,
                "Max Hit Points",
                200,
                ConfigDescriptions.Describe(
                    "门锁耐久上限",
                    "铺场时为每个目标随机分配耐久值的上限。",
                    new AcceptableValueRange<int>(1, 100000)));
            _shotgunWeapons = config.Bind(
                SectionBreach,
                "Shotgun Weapons",
                DefaultShotgunWeapons,
                ConfigDescriptions.Describe(
                    "霰弹枪白名单（模板 ID，逗号分隔）",
                    "允许破门的霰弹枪模板 ID 列表。"));
            _grenadeLauncherWeapons = config.Bind(
                SectionBreach,
                "Grenade Launcher Weapons",
                DefaultGrenadeLauncherWeapons,
                ConfigDescriptions.Describe(
                    "榴弹发射器白名单（模板 ID，逗号分隔）",
                    "允许破门的榴弹发射器模板 ID 列表。"));
            _meleeWeapons = config.Bind(
                SectionBreach,
                "Melee Weapons",
                DefaultMeleeWeapons,
                ConfigDescriptions.Describe(
                    "近战武器白名单（模板 ID，逗号分隔）",
                    "允许对非金属门生效的近战武器模板 ID 列表。"));
            _otherWeapons = config.Bind(
                SectionBreach,
                "Other Weapons",
                DefaultOtherWeapons,
                ConfigDescriptions.Describe(
                    "其他武器白名单（模板 ID，逗号分隔）",
                    "额外放行的武器模板 ID 列表（默认空）。"));
            _whitelistedRounds = config.Bind(
                SectionBreach,
                "Whitelisted Rounds",
                DefaultWhitelistedRounds,
                ConfigDescriptions.Describe(
                    "弹药白名单（模板 ID，逗号分隔）",
                    "允许破门的弹药模板 ID 列表。"));

            // 5. 爆炸
            _explosiveTimerSeconds = config.Bind(
                SectionExplosive,
                "Explosive Timer Seconds",
                10,
                ConfigDescriptions.Describe(
                    "C4 倒计时（秒）",
                    "放置 C4 后到引爆的等待秒数。",
                    new AcceptableValueRange<int>(1, 120)));
            _explosionDoesDamage = config.Bind(
                SectionExplosive,
                "Explosion Does Damage",
                false,
                ConfigDescriptions.Describe(
                    "爆炸造成伤害",
                    "C4 引爆是否对半径内的玩家造成伤害。"));
            _explosionRadius = config.Bind(
                SectionExplosive,
                "Explosion Radius",
                5,
                ConfigDescriptions.Describe(
                    "爆炸半径",
                    "C4 伤害判定半径（米）。",
                    new AcceptableValueRange<int>(0, 50)));
            _explosionDamage = config.Bind(
                SectionExplosive,
                "Explosion Damage",
                80,
                ConfigDescriptions.Describe(
                    "爆炸伤害",
                    "C4 引爆的基础伤害（按距离线性衰减）。",
                    new AcceptableValueRange<int>(0, 1000)));

            // 6. 排除与开关
            _blacklistedRooms = config.Bind(
                SectionExclusions,
                "Blacklisted Rooms",
                DefaultBlacklistedRooms,
                ConfigDescriptions.Describe(
                    "KeyId 黑名单（逗号分隔；默认空）",
                    "按门 KeyId 排除破门的房间列表（本 fork 默认空，偏离上游默认 12 项）。"));

            // 3. 破坏
            DestructionEnabled = config.Bind(
                SectionDestruction,
                "Destructible Doors",
                true,
                ConfigDescriptions.Describe(
                    "可破坏的门（总开关）",
                    "总开关：命中的支持门会被撕出孔洞，玩家与子弹均可看穿 / 射穿。脱落的板块掉落后留在原地，小碎片崩散。悬挂 / 脱落门为下方独立实验开关。Fika 存在时自动关闭（尚未同步）。"));
            ShowHoles = config.Bind(
                SectionDestruction,
                "Show Holes",
                true,
                ConfigDescriptions.Describe(
                    "显示孔洞",
                    "把破坏切入门本体模型与子弹碰撞体。关闭 = 仅模拟破坏（调试叠加显示），门保持完好；战局中途关闭会恢复门。"));
            HingeCollapse = config.Bind(
                SectionDestruction,
                "Hanging And Falling Doors (Experimental)",
                false,
                ConfigDescriptions.Describe(
                    "铰链脱落：悬挂 / 门板脱落（实验）",
                    "铰链可被射断：剩一个铰链时门悬挂其上，全断则脱出门框。仍较粗糙（可能卡住 / 穿模 / 表现异常）。关闭 = 铰链不可破坏，门保持为门；孔洞、坠落碎片与门闩仍照常工作。作用于新命中。"));
            LeafBlocksPlayers = config.Bind(
                SectionDestruction,
                "Hanging And Fallen Doors Block Players",
                true,
                ConfigDescriptions.Describe(
                    "悬挂 / 脱落门板阻挡玩家",
                    "（实验门）悬挂于一个铰链、或平铺地面的门对玩家与 bot 为实体（平铺的可跨过）。关闭 = 像小碎片一样可穿过。作用于之后脱落的门。"));
            HoleSize = config.Bind(
                SectionDestruction,
                "Hole Size",
                2f,
                ConfigDescriptions.Describe(
                    "孔洞大小",
                    "每次命中撕掉多少木料。1 = 5.56 弹撕出一块约 5 cm；2 = 每发约 10 cm、同点 10 发约 15 cm；3 = 极具破坏性。作用于新命中。",
                    new AcceptableValueRange<float>(0.5f, 3f)));
            ThroughPenWoodThin = config.Bind(
                SectionDestruction,
                "Punch-Through Penetration: Thin Wood",
                25f,
                ConfigDescriptions.Describe(
                    "击穿穿透：薄木",
                    "击穿薄木门所需的穿透力。较弱的弹药仍可损伤表面、门闩与铰链。",
                    new AcceptableValueRange<float>(1f, 100f)));
            ThroughPenWoodThick = config.Bind(
                SectionDestruction,
                "Punch-Through Penetration: Thick Wood",
                30f,
                ConfigDescriptions.Describe(
                    "击穿穿透：厚木",
                    "击穿厚木门所需的穿透力。",
                    new AcceptableValueRange<float>(1f, 100f)));
            ThroughPenMetalThin = config.Bind(
                SectionDestruction,
                "Punch-Through Penetration: Thin Metal",
                40f,
                ConfigDescriptions.Describe(
                    "击穿穿透：薄金属",
                    "击穿薄金属门所需的穿透力（仅孔洞与门闩 / 铰链损伤，无碎块）。",
                    new AcceptableValueRange<float>(1f, 100f)));
            CellSizeCm = config.Bind(
                SectionDestruction,
                "Fracture Cell Size (cm)",
                5f,
                ConfigDescriptions.Describe(
                    "破坏单元尺寸（cm）",
                    "门能脱落的碎片尺寸。更小 = 孔洞更精细，每扇门计算更重。作用于本局尚未受损的门。",
                    new AcceptableValueRange<float>(3f, 10f)));

            // 7. 调试
            ShowImpacts = config.Bind(
                SectionDebug,
                "Show Impacts",
                false,
                ConfigDescriptions.Describe(
                    "显示命中标记",
                    "在屏幕上标记近期门命中，含穿透力、伤害与是否会击穿。"));
            ShowDoorInfo = config.Bind(
                SectionDebug,
                "Show Door Info",
                false,
                ConfigDescriptions.Describe(
                    "显示门信息",
                    "显示所看门的 id、类型、材质，以及是否可破坏（及原因）。"));
            ShowDamageMap = config.Bind(
                SectionDebug,
                "Show Damage Map",
                false,
                ConfigDescriptions.Describe(
                    "显示破坏图谱",
                    "显示所看门模拟的破坏单元：血量、孔洞与将脱落的碎片。"));

            DoorBreachPlugin.Logger?.LogInfo(
                $"[Settings] 已绑定配置节：{SectionGeneral} / {SectionBreach} / {SectionDestruction} / " +
                $"{SectionExplosive} / {SectionExclusions} / {SectionDebug}" +
                $"（{SectionInteractions} 为后续工单预留）。");
        }

        /// <summary>从各 ConfigEntry 读值构造运行时配置快照。</summary>
        public static ModConfig BuildSnapshot()
        {
            return new ModConfig
            {
                ShotgunList = ParseList(_shotgunWeapons.Value),
                GrenadeLauncherList = ParseList(_grenadeLauncherWeapons.Value),
                MeleeWeaponList = ParseList(_meleeWeapons.Value),
                OtherWeaponList = ParseList(_otherWeapons.Value),
                BlacklistedRooms = ParseList(_blacklistedRooms.Value),
                WhitelistedRounds = ParseList(_whitelistedRounds.Value),
                PlebMode = _plebMode.Value,
                SemiPlebMode = _semiPlebMode.Value,
                BreachingRoundsOpenMetalDoors = _breachingRoundsOpenMetalDoors.Value,
                OpenLootableContainers = _openLootableContainers.Value,
                OpenCarDoors = _openCarDoors.Value,
                MinHitPoints = _minHitPoints.Value,
                MaxHitPoints = _maxHitPoints.Value,
                ExplosiveTimerInSec = _explosiveTimerSeconds.Value,
                ModExplosionStats = new ModConfig.ExplosionStats
                {
                    ExplosionDoesDamage = _explosionDoesDamage.Value,
                    ExplosionRadius = _explosionRadius.Value,
                    ExplosionDamage = _explosionDamage.Value,
                },
            };
        }

        /// <summary>逗号分隔列表解析：Trim 后丢弃空项。</summary>
        private static List<string> ParseList(string raw)
        {
            return (raw ?? string.Empty)
                .Split(',')
                .Select(item => item.Trim())
                .Where(item => item.Length > 0)
                .ToList();
        }
    }
}

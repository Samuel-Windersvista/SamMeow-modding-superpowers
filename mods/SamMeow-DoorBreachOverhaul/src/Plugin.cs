using System;
using BepInEx;
using BepInEx.Logging;
using DoorBreach.Models;
using DoorBreach.Patches;
using DoorOverhaul.Destruction.Patches;
using UnityEngine;

namespace DoorBreach
{
    /// <summary>
    /// 插件入口：生命周期（加载/卸载）、单一日志源、版本门禁、本地配置装配、补丁生命周期收口。
    /// </summary>
    /// <remarks>
    /// SPT 4.1.6 / EFT 0.16.9.40743 / Mono / BepInEx 5 形态：基类 <see cref="BaseUnityPlugin"/>，
    /// 入口 Awake()，卸载 OnDestroy()。配置不再由服务端下发（ADR-0002）。
    /// </remarks>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class DoorBreachPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.sammeow.doorbreach";
        public const string PluginName = "Door Breach Overhaul（破门大修）";
        public const string PluginVersion = "0.1.0";

        // 上游静态字段名保持不变（下游代码引用它们）。
        public static bool PlebMode;
        public static bool SemiPlebMode;
        public static bool BreachingRoundsOpenMetalDoors;
        public static bool OpenLootableContainers;
        public static bool OpenCarDoors;
        public static int MinHitPoints;
        public static int MaxHitPoints;
        public static int explosiveTimerInSec;
        public static bool explosionDoesDamage;
        public static int explosionRadius;
        public static int explosionDamage;

        public static ModConfig ModConfig { get; set; }

        /// <summary>Fika 桥保留位（子工程未并入，事件无人订阅时为空操作）。</summary>
        public static bool FikaInstalled { get; private set; }

        public enum GameObjectType
        {
            Door,
            Container,
            Trunk
        }

        /// <summary>全插件共享单一日志源（统一前缀 DoorBreachOverhaul）。</summary>
        internal static new ManualLogSource Logger { get; private set; }

        internal static DoorBreachPlugin Instance { get; private set; }

        private readonly PatchSet _patches = new PatchSet("DoorBreachOverhaul");

        /// <summary>破坏系统宿主（承载 DestructionController 的逐帧 Flush/Tick 与调试 GUI）。</summary>
        private GameObject _host;

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource("DoorBreachOverhaul");
            Instance = this;

            Logger.LogInfo(
                $"[DoorBreachOverhaul] {PluginVersion} 正在加载（目标 EFT {VersionGate.TargetEftVersion}）。");

            VersionGate.Check(Logger);

            Settings.Init(Config);

            RefreshConfigSnapshot();

            _patches.Enable(new ApplyHit());
            _patches.Enable(new ActionMenuDoorPatch());
            _patches.Enable(new ActionMenuKeyCardPatch());
            _patches.Enable(new PerfectCullingNullRefPatch());
            _patches.Enable(new OnGameStartedPatch());
            _patches.Enable(new DestructionBreachPatch());
            _patches.Enable(new DestructionGrenadePatch());
            _patches.Enable(new DestructionInteractGuardPatch());
            _patches.Enable(new DestructionSetStateGuardPatch());
            _patches.Enable(new DestructionKickGuardPatch());
            _patches.Enable(new DestructionPortalPatch());
            _patches.Enable(new DestructionCleanupPatch());

            // 破坏系统宿主：逐帧驱动 Flush/Tick 与破坏调试 GUI。
            _host = new GameObject("DoorBreachOverhaul");
            _host.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(_host);
            _host.AddComponent<DoorOverhaul.Destruction.DestructionController>();

            Logger.LogInfo($"[DoorBreachOverhaul] {PluginVersion} 已加载。");
        }

        private void OnDestroy()
        {
            // 补丁生命周期收口：不随插件卸载清理补丁属 modding standard STD-CLI-007 违规。
            _patches.DisableAll();

            if (_host != null)
            {
                Destroy(_host);
            }

            Logger?.LogInfo($"[DoorBreachOverhaul] {PluginVersion} 已卸载。");
            Instance = null;
        }

        /// <summary>
        /// 重读本地配置并重建运行时快照：插件启动一次 + 每个战局启动一次。
        /// F12 修改在下一个战局生效（战局中途不热更）；铺场与规则判定均消费该快照。
        /// </summary>
        internal static void RefreshConfigSnapshot()
        {
            try
            {
                ModConfig = Settings.BuildSnapshot();

                PlebMode = ModConfig.PlebMode;
                SemiPlebMode = ModConfig.SemiPlebMode;
                BreachingRoundsOpenMetalDoors = ModConfig.BreachingRoundsOpenMetalDoors;
                OpenLootableContainers = ModConfig.OpenLootableContainers;
                OpenCarDoors = ModConfig.OpenCarDoors;
                MinHitPoints = ModConfig.MinHitPoints;
                MaxHitPoints = ModConfig.MaxHitPoints;
                explosiveTimerInSec = ModConfig.ExplosiveTimerInSec;
                explosionDoesDamage = ModConfig.ModExplosionStats.ExplosionDoesDamage;
                explosionRadius = ModConfig.ModExplosionStats.ExplosionRadius;
                explosionDamage = ModConfig.ModExplosionStats.ExplosionDamage;

                Logger?.LogInfo(
                    $"[Settings] 配置快照：Pleb={PlebMode}, SemiPleb={SemiPlebMode}, " +
                    $"金属门破门弹={BreachingRoundsOpenMetalDoors}, 容器={OpenLootableContainers}, 后备箱={OpenCarDoors}, " +
                    $"耐久={MinHitPoints}-{MaxHitPoints}, 霰弹={ModConfig.ShotgunList.Count}, " +
                    $"榴弹={ModConfig.GrenadeLauncherList.Count}, 近战={ModConfig.MeleeWeaponList.Count}, " +
                    $"其他={ModConfig.OtherWeaponList.Count}, 弹药={ModConfig.WhitelistedRounds.Count}, " +
                    $"黑名单={ModConfig.BlacklistedRooms.Count}");
            }
            catch (Exception ex)
            {
                // 快照失败不阻断战局：保留上一份快照并记录。
                Logger?.LogError($"[Settings] 配置快照刷新失败（沿用上一份）：{ex}");
            }
        }
    }
}

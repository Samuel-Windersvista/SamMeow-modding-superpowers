using BepInEx.Configuration;
using System;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Spawned into the raid by <see cref="Patches.GameStartPatch"/>. Owns the radar HUD instance and
    /// the keyboard shortcuts that toggle it.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配（IL2CPP）：类型必须由 ClassInjector 注册，并带 IntPtr 构造。
    /// KeyboardShortcut 沿用 <c>BepInEx.Configuration.KeyboardShortcut</c>
    /// （BepInEx 6 已拆到 BepInEx.KeyboardShortcut.dll，其静态构造注册 Toml 转换器）。
    /// <para>
    /// [CRITICAL] 本组件由 GameStartPatch 挂在 **GameWorld 的 GameObject** 上。
    /// 因此所有失败路径只允许 <c>Destroy(this)</c>（销毁组件自身）——
    /// <c>Destroy(gameObject)</c> 会摧毁 GameWorld 对象，导致局内移动/武器/交互系统连锁崩坏
    /// （2026-09-15 实战事故根因之一）。上游 4.1 代码在失败路径使用 <c>Destroy(gameObject)</c>，
    /// 属继承缺陷；正常路径不触发，但雷达加载失败时必然引爆。
    /// </para>
    /// </remarks>
    public class InRaidRadarManager : MonoBehaviour
    {
        private const string FpsCameraName = "FPS Camera";

        private static GameObject _radarObject;

        /// <summary>The radar in the current raid, or null when there is nothing to notify.</summary>
        public static HaloRadar LiveRadar
        {
            get
            {
                if (_radarObject == null)
                    return null;

                HaloRadar radar = _radarObject.GetComponent<HaloRadar>();
                return radar != null && radar.InGame ? radar : null;
            }
        }

        private bool _radarKeyDown;
        private bool _corpseKeyDown;
        private bool _lootKeyDown;

        /// <summary>IL2CPP 注入类型必需：由运行时以原生指针构造代理。</summary>
        public InRaidRadarManager(System.IntPtr pointer) : base(pointer)
        {
        }

        private void Awake()
        {
            new Patches.LootItemAddPatch().Enable();
            new Patches.LootItemRemovePatch().Enable();
            new Patches.PlayerOnMakingShotPatch().Enable();

            GameObject playerCamera = GameObject.Find(FpsCameraName);
            if (playerCamera == null)
            {
                RadarPlugin.Log.LogError("FPS Camera not found");
                Destroy(this); // 仅移除本组件；宿主 GameObject 属于 GameWorld，不可销毁
                return;
            }

            // 5.0 适配（IL2CPP）：prefab 若因资产清理失效，直接 Instantiate 会抛 NRE 并中断
            // 整个 Awake（旧版日志只留下一条难解的 trampoline 异常）。这里显式护栏 + 明确日志。
            if (AssetFileManager.RadarHudPrefab == null)
            {
                RadarPlugin.Log.LogError("Radar HUD prefab is not loaded; radar disabled for this raid.");
                Destroy(this);
                return;
            }

            try
            {
                _radarObject = Instantiate(AssetFileManager.RadarHudPrefab);
            }
            catch (Exception e)
            {
                RadarPlugin.Log.LogError($"Radar HUD instantiate failed: {e.Message}");
                Destroy(this);
                return;
            }

            _radarObject.transform.SetParent(playerCamera.transform);
            _radarObject.AddComponent<HaloRadar>();

            RadarPlugin.Log.LogInfo("Radar instantiated");
        }

        private void OnEnable()
        {
            RadarConfig.Enabled.SettingChanged += OnRadarEnableChanged;
            UpdateRadarStatus();
        }

        private void OnDisable()
        {
            RadarConfig.Enabled.SettingChanged -= OnRadarEnableChanged;
        }

        private void Update()
        {
            ToggleOnKeyPress(RadarConfig.ToggleRadarKey, RadarConfig.Enabled, ref _radarKeyDown);
            ToggleOnKeyPress(RadarConfig.ToggleCorpseKey, RadarConfig.CorpseEnabled, ref _corpseKeyDown);
            ToggleOnKeyPress(RadarConfig.ToggleLootKey, RadarConfig.LootEnabled, ref _lootKeyDown);
        }

        private void OnRadarEnableChanged(object sender, EventArgs e) => UpdateRadarStatus();

        private void UpdateRadarStatus()
        {
            if (_radarObject == null)
            {
                RadarPlugin.Log.LogWarning("Radar did not load properly or has been destroyed");
                Destroy(this); // 仅移除本组件；宿主 GameObject 属于 GameWorld，不可销毁
                return;
            }

            _radarObject.SetActive(RadarConfig.Enabled.Value);
        }

        /// <summary>Flips a setting once per key press rather than once per frame it is held.</summary>
        private static void ToggleOnKeyPress(ConfigEntry<KeyboardShortcut> shortcut, ConfigEntry<bool> setting, ref bool wasDown)
        {
            bool isDown = shortcut.Value.IsDown();

            if (isDown && !wasDown)
                setting.Value = !setting.Value;

            wasDown = isDown;
        }
    }
}

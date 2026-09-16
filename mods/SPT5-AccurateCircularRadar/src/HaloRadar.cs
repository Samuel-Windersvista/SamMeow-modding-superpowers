using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System.Collections.Generic;
using UnityEngine;

namespace Radar
{
    /// <summary>
    /// Drives the radar: owns the HUD layout, the trackers and the per-frame update loop.
    /// Attached to the HUD prefab instance by <see cref="InRaidRadarManager"/>.
    /// </summary>
    /// <remarks>
    /// 4.1 -> 5.0 适配（IL2CPP）：
    /// <list type="bullet">
    /// <item>类型必须由 ClassInjector 注册，并带 IntPtr 构造（先例 tools/tarkov-runtime-bridge/src/PositionSampler.cs）。</item>
    /// <item>脉冲动画由协程改为 Update 驱动的计时：IL2CPP 下 <c>StartCoroutine(托管 IEnumerator)</c> 封送不可靠。
    /// 行为等价（每 <c>_pulseInterval</c> 秒一圈，角度从 360 递减到 0）。</item>
    /// <item><c>AllPlayersEverExisted</c> 是 Il2Cpp 序列，去掉托管 LINQ 的 <c>Count()</c>。</item>
    /// </list>
    /// </remarks>
    public class HaloRadar : MonoBehaviour
    {
        private const string FpsCameraName = "FPS Camera";

        /// <summary>Fire Mode sweeps players far more often, so muzzle flashes register promptly.</summary>
        private const float FireModeScanInterval = 0.1f;

        /// <summary>Slowest the pulse sweep may run, in seconds per revolution.</summary>
        private const float MinPulseInterval = 1f;

        /// <summary>False until a raid is live, so the Harmony patches know not to call in.</summary>
        public bool InGame { get; private set; }

        private readonly Dictionary<string, BlipPlayer> _players = new Dictionary<string, BlipPlayer>();

        private GameWorld _gameWorld;
        private Player _player;
        private RadarHudLayout _layout;
        private LootTracker _loot;
        private MinefieldTracker _minefield;
        private ExfilTracker _exfil;

        private bool _ready;
        private bool _pulseActive;
        private float _pulseProgress;
        private float _pulseInterval = MinPulseInterval;
        private float _lastPlayerScanTime;

        /// <summary>IL2CPP 注入类型必需：由运行时以原生指针构造代理。</summary>
        public HaloRadar(System.IntPtr pointer) : base(pointer)
        {
        }

        // --- Called by the Harmony patches -------------------------------------------------------

        public void AddLoot(string id, Item item, Transform transform, bool lazyUpdate = false) =>
            _loot.Add(id, item, transform, lazyUpdate);

        public void RemoveLootByKey(int key) => _loot.RemoveByKey(key);

        public void UpdateFireTime(string profileId)
        {
            if (_players.TryGetValue(profileId, out BlipPlayer blip))
                blip.UpdateLastFireTime(Time.time);
        }

        // --- Unity lifecycle ---------------------------------------------------------------------

        private void Awake()
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                RadarPlugin.Log.LogWarning("GameWorld singleton not found.");
                Destroy(gameObject);
                return;
            }

            _gameWorld = Singleton<GameWorld>.Instance;
            if (_gameWorld.MainPlayer == null)
            {
                RadarPlugin.Log.LogWarning("MainPlayer is null.");
                Destroy(gameObject);
                return;
            }

            _player = _gameWorld.MainPlayer;

            _layout = new RadarHudLayout(transform, GameObject.Find(FpsCameraName));
            _loot = new LootTracker(_gameWorld, _player);
            _minefield = new MinefieldTracker(_gameWorld);
            _exfil = new ExfilTracker(_player);

            RadarRegion.FillColor = RadarConfig.MinefieldColor.Value;

            ItemPricing.Init();

            // Destroy() only takes effect at the end of the frame, so the bail-outs above still let
            // OnEnable and Update run once. This gate keeps them from touching half-built state.
            _ready = true;
            RadarPlugin.Log.LogInfo("Radar loaded");
        }

        private void OnEnable()
        {
            if (!_ready) return;

            InitRadar();
            RadarPlugin.Instance.Config.SettingChanged += OnSettingChanged;
            ApplyAllSettings();
            InGame = true;
        }

        private void OnDisable()
        {
            if (!_ready) return;

            RadarPlugin.Instance.Config.SettingChanged -= OnSettingChanged;
            StopPulse();
            InGame = false;
        }

        private void Update()
        {
            if (!_ready || _player == null) return;

            TickPulse();

            if (!RadarConfig.CompassEnabled.Value)
                _layout.UpdateBorderRotation();

            _loot.Tick();
            Render(positionUpdate: ScanPlayers());

            if (RadarConfig.CompassEnabled.Value)
                _layout.TickCompass();
        }

        // --- Radar state -------------------------------------------------------------------------

        private void InitRadar()
        {
            _layout.Apply();

            if (RadarConfig.LootEnabled.Value)
                _loot.Rebuild();

            _minefield.Rebuild();
            _exfil.Rebuild();
        }

        /// <summary>
        /// Registers any players that have appeared since the last sweep.
        /// </summary>
        /// <returns>False when the sweep was throttled, meaning blips should hold their position.</returns>
        private bool ScanPlayers()
        {
            float interval = RadarConfig.FireModeEnabled.Value
                ? FireModeScanInterval
                : RadarConfig.ScanInterval.Value;

            if (Time.time - _lastPlayerScanTime < interval)
                return false;

            _lastPlayerScanTime = Time.time;

            int totalPlayers = 0;
            foreach (Player player in _gameWorld.AllPlayersEverExisted)
                totalPlayers++;

            // Everyone is already tracked apart from us, so there is nothing to register.
            if (totalPlayers == _players.Count + 1)
                return true;

            foreach (Player player in _gameWorld.AllPlayersEverExisted)
            {
                if (player == null || player == _player)
                    continue;

                if (!_players.ContainsKey(player.ProfileId))
                    _players[player.ProfileId] = new BlipPlayer(player);
            }

            return true;
        }

        private void Render(bool positionUpdate)
        {
            Target.SetPlayerTransform(_player.Transform);
            Target.SetRadarRange(RadarConfig.InnerRange.Value, RadarConfig.OuterRange.Value);
            RadarRegion.SetPlayerPosition(_player.Transform.position);

            foreach (BlipPlayer blip in _players.Values)
                blip.Update(positionUpdate);

            _loot.Render();
            _minefield.Render();
            _exfil.Render();
        }

        // --- Settings ----------------------------------------------------------------------------

        /// <summary>Pushes every setting into the HUD, for start-up and after a mode switch.</summary>
        private void ApplyAllSettings()
        {
            _pulseInterval = Mathf.Max(MinPulseInterval, RadarConfig.ScanInterval.Value);
            TogglePulse(RadarConfig.PulseEnabled.Value);
            _layout.ApplyPosition();
            _layout.ApplyScale();
        }

        private void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            // The HUD is torn down while the radar is toggled off; nothing to push settings into.
            if (!gameObject.activeInHierarchy) return;

            _pulseInterval = Mathf.Max(MinPulseInterval, RadarConfig.ScanInterval.Value);

            ConfigEntryBase changed = e.ChangedSetting;

            if (changed == RadarConfig.PulseEnabled)
                TogglePulse(RadarConfig.PulseEnabled.Value);
            else if (changed == RadarConfig.BackgroundColor)
                _layout.SetBackgroundColor(RadarConfig.BackgroundColor.Value);
            else if (changed == RadarConfig.CompassEnabled)
                InitRadar();
            else if (changed == RadarConfig.MinefieldEnabled)
                _minefield.Rebuild();
            else if (changed == RadarConfig.ExfilEnabled)
                _exfil.Rebuild();
            else if (changed == RadarConfig.MinefieldColor)
                RadarRegion.FillColor = RadarConfig.MinefieldColor.Value;
            else if (changed == RadarConfig.LootEnabled || changed == RadarConfig.WishlistLootEnabled ||
                     changed == RadarConfig.LootValuePerSlot || changed == RadarConfig.LootThreshold)
                RefreshLoot();
            else if (changed == RadarConfig.OffsetX || changed == RadarConfig.OffsetY)
                _layout.ApplyPosition();
            else if (changed == RadarConfig.HudSize)
                _layout.ApplyScale();
        }

        private void RefreshLoot()
        {
            if (RadarConfig.LootEnabled.Value)
                _loot.Rebuild();
            else
                _loot.Clear();
        }

        // --- Pulse animation ---------------------------------------------------------------------

        private void TogglePulse(bool enable)
        {
            StopPulse();

            _pulseActive = enable;
            _pulseProgress = 0f;

            _layout.SetPulseVisible(enable);
        }

        private void StopPulse()
        {
            _pulseActive = false;
            _pulseProgress = 0f;
        }

        /// <summary>Advances the pulse sweep; one revolution per pulse interval, 360 -> 0 degrees.</summary>
        private void TickPulse()
        {
            if (!_pulseActive) return;

            _pulseProgress += Time.deltaTime / _pulseInterval;
            if (_pulseProgress >= 1f)
                _pulseProgress -= Mathf.Floor(_pulseProgress);

            _layout.SetPulseAngle((1f - _pulseProgress) * 360f);
        }
    }
}

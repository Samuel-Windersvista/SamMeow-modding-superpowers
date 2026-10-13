using System;
using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using UnityEngine;

namespace SamMeow.DebugToolkit.Modules.QoL
{
    /// <summary>
    /// 战局内便利组组件宿主：无限体力（每帧）+ 无限能量 / 水分（事件驱动）+
    /// 换弹 / 卸弹速度（全局配置改写）。移植 DadGamerMode 1.9.3
    /// <c>Features/MaxStamina.cs</c> / <c>Energy.cs</c> / <c>Hydration.cs</c> / <c>MagReloadSpeed.cs</c>。
    /// </summary>
    /// <remarks>
    /// 0.16.9 触点（见 <c>docs/api-touchpoints/dadgamer.md</c> §1.4–1.6）：
    /// <list type="bullet">
    /// <item><c>Player.Physical</c>（<c>PhysicalBase</c>）→ <c>Stamina / HandsStamina / Oxygen</c>，
    /// <c>.Current</c> 为 public 字段，<c>.TotalCapacity</c> 为 <c>Compute&lt;float&gt;</c>（<c>.Value</c>）；</item>
    /// <item><c>ActiveHealthController.Energy / Hydration</c> 现为 <c>ValueStruct</c>（<c>.Maximum</c> 字段），
    /// <c>ChangeEnergy / ChangeHydration</c> 为 public 非虚；<c>EnergyChangedEvent / HydrationChangedEvent</c>
    /// 为 <c>Action&lt;float&gt;</c>（可原生订阅）；</item>
    /// <item>换弹速度真名 <c>EFT.GlobalConfiguration.BaseLoadTime / BaseUnloadTime</c>（public 字段，默认 0.85 / 0.3）。</item>
    /// </list>
    /// 宿主是 GameWorld；清理仅 <c>Destroy(this)</c>，绝不摧毁宿主 GameObject。
    /// </remarks>
    public sealed class QoLRaidComponent : MonoBehaviour
    {
        private const float DefaultReloadSpeed = 0.85f;
        private const float DefaultUnloadSpeed = 0.3f;

        private static QoLRaidComponent _instance;

        private Player _player;
        private ActiveHealthController _healthController;
        private bool _healthSubscribed;
        private bool _configSubscribed;

        /// <summary>Unity 反射实例化所需的 public 无参构造。</summary>
        public QoLRaidComponent()
        {
        }

        /// <summary>
        /// 在 <c>GameWorld.OnGameStarted</c> 时把本组件挂到 GameWorld GameObject 上（幂等）。
        /// </summary>
        internal static void EnsureAttached(GameWorld world)
        {
            if (world == null)
            {
                return;
            }

            GameObject host = world.gameObject;
            if (host == null)
            {
                return;
            }

            QoLRaidComponent existing = host.GetComponent<QoLRaidComponent>();
            if (existing == null)
            {
                existing = host.AddComponent<QoLRaidComponent>();
            }

            _instance = existing;
        }

        /// <summary>模块停用时移除战局组件（只摧毁组件，绝不摧毁 GameWorld GameObject）。</summary>
        internal static void DestroyInstance()
        {
            QoLRaidComponent instance = _instance;
            _instance = null;

            if (instance != null)
            {
                Destroy(instance);
            }
        }

        /// <summary>模块停用时还原换弹 / 卸弹全局默认（不触碰用户配置值）。</summary>
        internal static void RestoreReloadDefaults()
        {
            try
            {
                if (!Singleton<GlobalConfiguration>.Instantiated)
                {
                    return;
                }

                GlobalConfiguration config = Singleton<GlobalConfiguration>.Instance;
                if (config != null)
                {
                    config.BaseLoadTime = DefaultReloadSpeed;
                    config.BaseUnloadTime = DefaultUnloadSpeed;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 还原换弹速度默认失败（已忽略）：{ex.Message}");
            }
        }

        private void OnEnable()
        {
            SubscribeConfig();
            ApplyGlobalReloadSpeed();
        }

        private void Update()
        {
            try
            {
                if (!QoLSettings.Ready)
                {
                    return;
                }

                if (!_configSubscribed)
                {
                    SubscribeConfig();
                }

                if (_healthController == null && !TryResolvePlayer())
                {
                    return;
                }

                if (!_healthSubscribed)
                {
                    SubscribeHealth();
                }

                ApplyInfiniteStamina();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[QoL] 战局组件 Update 异常（已忽略）：{ex.Message}");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeHealth();
            UnsubscribeConfig();

            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>惰性解析主玩家与其健康控制器；未进局 / 已撤离返回 false。</summary>
        private bool TryResolvePlayer()
        {
            GameWorld world;
            try
            {
                world = Singleton<GameWorld>.Instance;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] GameWorld 单例读取失败：{ex.Message}");
                return false;
            }

            if (world == null)
            {
                return false;
            }

            Player current = world.MainPlayer;
            if (current == null)
            {
                return false;
            }

            _player = current;
            _healthController = current.ActiveHealthController;
            return _healthController != null;
        }

        /// <summary>无限体力：身体体力 / 手部体力 / 氧气每帧回满（与上游 Update 一致）。</summary>
        private void ApplyInfiniteStamina()
        {
            if (!QoLSettings.InfiniteStamina.Value || _player == null)
            {
                return;
            }

            PhysicalBase physical = _player.Physical;
            if (physical == null)
            {
                return;
            }

            physical.Stamina.Current = physical.Stamina.TotalCapacity.Value;
            physical.HandsStamina.Current = physical.HandsStamina.TotalCapacity.Value;
            physical.Oxygen.Current = physical.Oxygen.TotalCapacity.Value;
        }

        private void SubscribeHealth()
        {
            if (_healthSubscribed || _healthController == null)
            {
                return;
            }

            _healthController.EnergyChangedEvent += OnEnergyChanged;
            _healthController.HydrationChangedEvent += OnHydrationChanged;
            _healthSubscribed = true;
        }

        private void UnsubscribeHealth()
        {
            try
            {
                if (_healthController != null)
                {
                    _healthController.EnergyChangedEvent -= OnEnergyChanged;
                    _healthController.HydrationChangedEvent -= OnHydrationChanged;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 体力 / 能量事件退订失败（已忽略）：{ex.Message}");
            }
            finally
            {
                _healthSubscribed = false;
            }
        }

        /// <summary>能量变化回调：开关开启时即刻回满（与上游 EnergyChangedEvent 一致）。</summary>
        private void OnEnergyChanged(float amount)
        {
            try
            {
                if (!QoLSettings.InfiniteEnergy.Value || _healthController == null)
                {
                    return;
                }

                _healthController.ChangeEnergy(_healthController.Energy.Maximum);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[QoL] 无限能量回调异常（已忽略）：{ex.Message}");
            }
        }

        /// <summary>水分变化回调：开关开启时即刻回满（与上游 HydrationChangedEvent 一致）。</summary>
        private void OnHydrationChanged(float amount)
        {
            try
            {
                if (!QoLSettings.InfiniteHydration.Value || _healthController == null)
                {
                    return;
                }

                _healthController.ChangeHydration(_healthController.Hydration.Maximum);
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[QoL] 无限水分回调异常（已忽略）：{ex.Message}");
            }
        }

        private void SubscribeConfig()
        {
            if (_configSubscribed || !QoLSettings.Ready)
            {
                return;
            }

            if (QoLSettings.ReloadUnloadToggle != null)
            {
                QoLSettings.ReloadUnloadToggle.SettingChanged += OnToggleReloadUnloadChanged;
            }

            if (QoLSettings.ReloadMagSpeed != null)
            {
                QoLSettings.ReloadMagSpeed.SettingChanged += OnReloadSpeedChanged;
            }

            if (QoLSettings.UnloadMagSpeed != null)
            {
                QoLSettings.UnloadMagSpeed.SettingChanged += OnUnloadSpeedChanged;
            }

            _configSubscribed = true;
        }

        private void UnsubscribeConfig()
        {
            try
            {
                if (QoLSettings.ReloadUnloadToggle != null)
                {
                    QoLSettings.ReloadUnloadToggle.SettingChanged -= OnToggleReloadUnloadChanged;
                }

                if (QoLSettings.ReloadMagSpeed != null)
                {
                    QoLSettings.ReloadMagSpeed.SettingChanged -= OnReloadSpeedChanged;
                }

                if (QoLSettings.UnloadMagSpeed != null)
                {
                    QoLSettings.UnloadMagSpeed.SettingChanged -= OnUnloadSpeedChanged;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 换弹速度配置退订失败（已忽略）：{ex.Message}");
            }
            finally
            {
                _configSubscribed = false;
            }
        }

        private void OnReloadSpeedChanged(object sender, EventArgs e)
        {
            ApplyGlobalReloadSpeed();
        }

        private void OnUnloadSpeedChanged(object sender, EventArgs e)
        {
            ApplyGlobalReloadSpeed();
        }

        /// <summary>
        /// 开关变更：关闭时还原全局默认并复位配置值（上游 <c>MagReloadSpeed</c> 语义）；
        /// 随后按当前状态重写全局值。
        /// </summary>
        private void OnToggleReloadUnloadChanged(object sender, EventArgs e)
        {
            try
            {
                if (QoLSettings.Ready && !QoLSettings.ReloadUnloadToggle.Value)
                {
                    QoLSettings.ReloadMagSpeed.Value = DefaultReloadSpeed;
                    QoLSettings.UnloadMagSpeed.Value = DefaultUnloadSpeed;
                }

                ApplyGlobalReloadSpeed();
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 换弹速度开关变更处理失败（已忽略）：{ex.Message}");
            }
        }

        /// <summary>按当前开关 / 配置值改写全局换弹 / 卸弹速度；关闭时写默认值。</summary>
        private static void ApplyGlobalReloadSpeed()
        {
            try
            {
                if (!QoLSettings.Ready || !Singleton<GlobalConfiguration>.Instantiated)
                {
                    return;
                }

                GlobalConfiguration config = Singleton<GlobalConfiguration>.Instance;
                if (config == null)
                {
                    return;
                }

                if (QoLSettings.ReloadUnloadToggle.Value)
                {
                    config.BaseLoadTime = QoLSettings.ReloadMagSpeed.Value;
                    config.BaseUnloadTime = QoLSettings.UnloadMagSpeed.Value;
                }
                else
                {
                    config.BaseLoadTime = DefaultReloadSpeed;
                    config.BaseUnloadTime = DefaultUnloadSpeed;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 应用换弹速度失败（已忽略）：{ex.Message}");
            }
        }
    }
}

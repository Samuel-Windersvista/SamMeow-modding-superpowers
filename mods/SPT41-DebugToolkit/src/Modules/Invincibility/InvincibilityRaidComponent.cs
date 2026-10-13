using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using UnityEngine;

namespace SamMeow.DebugToolkit.Modules.Invincibility
{
    /// <summary>
    /// 战局内无敌组件宿主：无坠落伤害（写 <c>FallSafeHeight</c>）与 CODMode 延迟自动治疗 +
    /// 负面效果清理。移植 DadGamerMode 1.9.3 <c>Features/CODMode.cs</c> 与
    /// <c>Features/FallingDamage.cs</c>。
    /// </summary>
    /// <remarks>
    /// Mono / BepInEx 5 形态（与 5.0 线 IL2CPP 版的关键差异）：
    /// <list type="bullet">
    /// <item>托管事件可原生订阅：<c>Player.BeingHitAction</c> / <c>Player.OnPlayerDeadOrUnspawn</c> /
    /// <c>ActiveHealthController.EffectAddedEvent</c>（0.16.9 触点为 <c>Action&lt;IHealthEffect&gt;</c>）；</item>
    /// <item>效果清理按上游语义：<c>EffectAddedEvent</c> 处理器排除 <c>IDesirable</c> / <c>IPermanent</c>
    /// 后调 <c>RemoveEffectFromList(ActiveHealthController.Effect)</c>；</item>
    /// <item>治疗写 <c>BodyState[limb].Health</c>（<c>HealthValue</c> 为引用类型，<c>Current</c> 虚属性
    /// 写回生效）；</item>
    /// <item>无坠落伤害写属性 <c>FallSafeHeight</c>（后备字段 <c>_fallSafeHeight</c> 在 0.16.9 为 private，
    /// 不可直写）；写策略为「开启时写、关闭时还原」，不做每帧持续覆盖。</item>
    /// </list>
    /// 宿主是 GameWorld；清理只 <c>Destroy(this)</c>，绝不摧毁宿主 GameObject。
    /// </remarks>
    public sealed class InvincibilityRaidComponent : MonoBehaviour
    {
        /// <summary>CODMode 治疗 / 清理的作用部位（与上游一致，不含 Common）。</summary>
        private static readonly EBodyPart[] CodBodyParts =
        {
            EBodyPart.Stomach,
            EBodyPart.Chest,
            EBodyPart.Head,
            EBodyPart.RightLeg,
            EBodyPart.LeftLeg,
            EBodyPart.LeftArm,
            EBodyPart.RightArm
        };

        /// <summary>治疗检查节流：与上游一致，每 60 帧一次。</summary>
        private const int TicksPerHealCheck = 60;

        private const float DefaultFallSafeHeight = 1.8f;
        private const float NoFallSafeHeight = 999999f;

        private static InvincibilityRaidComponent _instance;

        private Player _player;
        private ActiveHealthController _healthController;
        private bool _subscribed;

        /// <summary>玩家已死亡 / 离场后置位，停止后续处理直至组件销毁。</summary>
        private bool _playerGone;

        /// <summary>本组件是否已把 <c>FallSafeHeight</c> 写为 <see cref="NoFallSafeHeight"/>（用于一次性还原）。</summary>
        private bool _noFallingDamageActive;

        private int _frameCount;
        private float _timeSinceLastHit;
        private bool _isRegenerating;

        /// <summary>Unity 反射实例化所需的 public 无参构造。</summary>
        public InvincibilityRaidComponent()
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

            InvincibilityRaidComponent existing = host.GetComponent<InvincibilityRaidComponent>();
            if (existing == null)
            {
                existing = host.AddComponent<InvincibilityRaidComponent>();
            }

            _instance = existing;
        }

        /// <summary>模块停用时移除战局组件（只摧毁组件，绝不摧毁 GameWorld GameObject）。</summary>
        internal static void DestroyInstance()
        {
            InvincibilityRaidComponent instance = _instance;
            _instance = null;

            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private void Update()
        {
            try
            {
                if (!InvincibilityConfig.Ready || _playerGone)
                {
                    return;
                }

                if (_healthController == null && !TryResolvePlayer())
                {
                    return;
                }

                if (!_subscribed)
                {
                    Subscribe();
                }

                ApplyFallSafeHeight();

                if (InvincibilityConfig.CODMode.Value)
                {
                    UpdateCODMode();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Invincibility] 战局组件 Update 异常（已忽略）：{ex.Message}");
            }
        }

        private void OnDestroy()
        {
            Unsubscribe();

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
                Plugin.Logger?.LogWarning($"[Invincibility] GameWorld 单例读取失败：{ex.Message}");
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

        private void Subscribe()
        {
            if (_subscribed || _player == null || _healthController == null)
            {
                return;
            }

            _player.OnPlayerDeadOrUnspawn += OnPlayerDeadOrUnspawn;
            _player.BeingHitAction += OnBeingHit;
            _healthController.EffectAddedEvent += OnEffectAdded;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            try
            {
                if (_player != null)
                {
                    _player.OnPlayerDeadOrUnspawn -= OnPlayerDeadOrUnspawn;
                    _player.BeingHitAction -= OnBeingHit;
                }

                if (_healthController != null)
                {
                    _healthController.EffectAddedEvent -= OnEffectAdded;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[Invincibility] 事件退订失败（已忽略）：{ex.Message}");
            }
            finally
            {
                _subscribed = false;
            }
        }

        /// <summary>
        /// 无坠落伤害：开启时写 <see cref="NoFallSafeHeight"/>，关闭时一次性还原
        /// <see cref="DefaultFallSafeHeight"/>；稳态不重复覆盖。
        /// </summary>
        private void ApplyFallSafeHeight()
        {
            if (_healthController == null)
            {
                return;
            }

            if (InvincibilityConfig.NoFallingDamage.Value)
            {
                if (!_noFallingDamageActive)
                {
                    _healthController.FallSafeHeight = NoFallSafeHeight;
                    _noFallingDamageActive = true;
                }

                return;
            }

            if (_noFallingDamageActive)
            {
                _healthController.FallSafeHeight = DefaultFallSafeHeight;
                _noFallingDamageActive = false;
            }
        }

        /// <summary>上游 Update：累加受击计时，每 60 帧检查一次等待阈值并治疗。</summary>
        private void UpdateCODMode()
        {
            _timeSinceLastHit += Time.unscaledDeltaTime;

            _frameCount++;
            if (_frameCount < TicksPerHealCheck)
            {
                return;
            }

            _frameCount = 0;

            if (_timeSinceLastHit >= InvincibilityConfig.HealWait.Value)
            {
                _isRegenerating = true;
                StartHealing();
            }
        }

        /// <summary>逐个可治疗部位恢复血量；跳过已摧毁部位（与上游一致）。</summary>
        private void StartHealing()
        {
            if (!_isRegenerating || !InvincibilityConfig.CODMode.Value || _healthController == null)
            {
                return;
            }

            float healRate = InvincibilityConfig.HealRate.Value;
            var bodyState = _healthController.BodyState;
            if (bodyState == null)
            {
                return;
            }

            foreach (EBodyPart limb in CodBodyParts)
            {
                try
                {
                    if (!bodyState.TryGetValue(limb, out var part) || part == null || part.IsDestroyed)
                    {
                        continue;
                    }

                    HealthValue health = part.Health;
                    if (health == null || health.AtMaximum)
                    {
                        continue;
                    }

                    // HealthValue 为引用类型，Current 为虚属性 setter（Mono 直调，写回控制器）。
                    health.Current += healRate;
                }
                catch (Exception ex)
                {
                    Plugin.Logger?.LogError(
                        $"[Invincibility] CODMode 治疗部位 {limb} 失败（已跳过）：{ex.Message}");
                }
            }
        }

        /// <summary>
        /// 效果新增回调：CODMode 且不保留流血时，清理非 <c>IDesirable</c> / 非 <c>IPermanent</c>
        /// 的负面效果（上游 <c>EffectAddedEvent</c> 语义）。
        /// </summary>
        private void OnEffectAdded(IHealthEffect effect)
        {
            try
            {
                if (!InvincibilityConfig.Ready || effect == null)
                {
                    return;
                }

                if (!InvincibilityConfig.CODMode.Value || InvincibilityConfig.BleedingDamage.Value)
                {
                    return;
                }

                if (effect is IDesirable || effect is IPermanent)
                {
                    return;
                }

                ActiveHealthController.Effect nativeEffect = effect as ActiveHealthController.Effect;
                if (nativeEffect != null)
                {
                    _healthController?.RemoveEffectFromList(nativeEffect);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Invincibility] 效果清理回调异常（已忽略）：{ex.Message}");
            }
        }

        /// <summary>受击回调：复位计时并退出治疗状态（上游 <c>BeingHitAction</c> 处理器）。</summary>
        private void OnBeingHit(DamageInfo damageInfo, EBodyPart bodyPart, float amount)
        {
            try
            {
                _timeSinceLastHit = 0f;
                _isRegenerating = false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[Invincibility] 受击回调异常（已忽略）：{ex.Message}");
            }
        }

        /// <summary>玩家死亡 / 离场：退订并停止后续处理。</summary>
        private void OnPlayerDeadOrUnspawn(Player player)
        {
            try
            {
                Unsubscribe();
                _playerGone = true;
                _healthController = null;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[Invincibility] 死亡回调异常（已忽略）：{ex.Message}");
            }
        }
    }
}

using System;
using Comfort.Common;

namespace SamMeow.DebugToolkit.Modules.BotOverlay
{
    /// <summary>
    /// Bot 调试覆盖层模块注册点（移植自 DrakiaXYZ-BotDebug 1.8.0 / BigBrain 1.5.0）。
    /// </summary>
    /// <remarks>
    /// 生命周期：
    /// <list type="bullet">
    /// <item><see cref="Enable"/> = 绑定配置 + 逐项订阅 Enable/ActiveMode 变更 + 装配
    /// <c>GameWorld.OnGameStarted</c> 补丁（每局挂载覆盖层组件）；</item>
    /// <item><see cref="Disable"/> = 逐项退订 + 撤销补丁 + 移除组件。</item>
    /// </list>
    /// 补丁生命周期经共享 <see cref="PatchSet"/>；配置订阅逐项 try/catch。
    /// <see cref="Disable"/> 无条件清理（不得因 <c>_enabled == false</c> 早退），覆盖部分装配状态。
    /// 补丁与组件异常一律 fail-open，不穿透游戏调用链。
    /// </remarks>
    internal static class BotOverlayModule
    {
        private static readonly PatchSet Patches = new PatchSet("BotOverlay");

        private static bool _enabled;
        private static EventHandler _enableChangedHandler;
        private static EventHandler _activeModeChangedHandler;

        public static void Enable()
        {
            if (_enabled)
            {
                return;
            }

            BotOverlaySettings.Init(Settings.Config);

            try
            {
                if (BotOverlaySettings.Enable != null)
                {
                    _enableChangedHandler = OnEnableChanged;
                    BotOverlaySettings.Enable.SettingChanged += _enableChangedHandler;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] Enable 配置订阅失败：{ex.Message}");
            }

            try
            {
                if (BotOverlaySettings.ActiveMode != null)
                {
                    _activeModeChangedHandler = OnActiveModeChanged;
                    BotOverlaySettings.ActiveMode.SettingChanged += _activeModeChangedHandler;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] ActiveMode 配置订阅失败：{ex.Message}");
            }

            Patches.Enable(new GameStartPatch());

            _enabled = true;
            Plugin.Logger?.LogInfo("[BotOverlay] 模块已启用。");
        }

        public static void Disable()
        {
            // 无条件清理：订阅逐项退订 + 补丁逐项撤销（即使 Enable 曾中途失败）。

            try
            {
                if (BotOverlaySettings.Enable != null && _enableChangedHandler != null)
                {
                    BotOverlaySettings.Enable.SettingChanged -= _enableChangedHandler;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] Enable 配置退订失败：{ex.Message}");
            }
            finally
            {
                _enableChangedHandler = null;
            }

            try
            {
                if (BotOverlaySettings.ActiveMode != null && _activeModeChangedHandler != null)
                {
                    BotOverlaySettings.ActiveMode.SettingChanged -= _activeModeChangedHandler;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] ActiveMode 配置退订失败：{ex.Message}");
            }
            finally
            {
                _activeModeChangedHandler = null;
            }

            Patches.DisableAll();
            BotDebugComponent.Disable();

            _enabled = false;
            Plugin.Logger?.LogInfo("[BotOverlay] 模块已停用。");
        }

        private static void OnEnableChanged(object sender, EventArgs e)
        {
            try
            {
                if (!BotOverlaySettings.Initialized)
                {
                    return;
                }

                // 不在战局中则无需处理：下一局由 OnGameStarted 补丁挂载。
                if (!Singleton<IBotGame>.Instantiated)
                {
                    return;
                }

                if (BotOverlaySettings.Enable.Value)
                {
                    BotDebugComponent.Enable();
                }
                else
                {
                    BotDebugComponent.Disable();
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] Enable 配置变更处理失败（已忽略）：{ex.Message}");
            }
        }

        private static void OnActiveModeChanged(object sender, EventArgs e)
        {
            try
            {
                if (!BotOverlaySettings.TryParseMode(BotOverlaySettings.ActiveMode.Value, out _))
                {
                    Plugin.Logger?.LogWarning(
                        $"[BotOverlay] ActiveMode 值 '{BotOverlaySettings.ActiveMode.Value}' 无效；已回退 Behaviour。");
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[BotOverlay] ActiveMode 校验失败（已忽略）：{ex.Message}");
            }
        }
    }
}

using System;
using System.Diagnostics;
using BepInEx;
using BepInEx.Logging;
using SamMeow.DebugToolkit.Modules.BotOverlay;
using SamMeow.DebugToolkit.Modules.Invincibility;
using SamMeow.DebugToolkit.Modules.QoL;
using SamMeow.DebugToolkit.Modules.Tooltip;

namespace SamMeow.DebugToolkit
{
    /// <summary>
    /// 插件入口：生命周期（加载/卸载）、版本门禁、单一日志源、模块装配。
    /// </summary>
    /// <remarks>
    /// SPT 4.1.6 / Mono / BepInEx 5 形态：基类为 <see cref="BaseUnityPlugin"/>，入口方法为 Awake()，
    /// 卸载回调为 OnDestroy()。与 5.0 线（IL2CPP / BepInEx 6：BasePlugin + Load()/Unload()）独立，
    /// 不可照搬其运行时写法。
    /// </remarks>
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("xyz.drakia.bigbrain", "1.5.0")]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.sammeow.debugtoolkit";
        public const string Name = "SamMeow.DebugToolkit";
        public const string Version = "0.1.0";

        /// <summary>目标 EFT 客户端版本（SPT 4.1.6 基线）。仅用于软门禁比对。</summary>
        internal const string TargetEftVersion = "0.16.9.40743";

        /// <summary>目标 EFT 构建号（可执行文件 FileVersionInfo.FilePrivatePart）。</summary>
        internal const int TargetEftBuild = 40743;

        internal static Plugin Instance { get; private set; }

        /// <summary>
        /// 全插件共享单一日志源（显式创建，统一前缀）；其余类经 <see cref="Plugin.Logger"/> 取用。
        /// </summary>
        internal static new ManualLogSource Logger { get; private set; }

        private void Awake()
        {
            Logger = BepInEx.Logging.Logger.CreateLogSource(Name);
            Instance = this;

            Logger.LogInfo($"[{Name}] {Version} 正在加载（目标 EFT {TargetEftVersion}）。");

            WarnIfVersionMismatch();

            Settings.Init(Config);

            // 模块装配顺序 Tooltip -> BotOverlay -> Invincibility -> QoL（卸载逆序）。
            TooltipModule.Enable();
            BotOverlayModule.Enable();
            InvincibilityModule.Enable();
            QoLModule.Enable();

            Logger.LogInfo($"[{Name}] {Version} 已加载。");
        }

        private void OnDestroy()
        {
            // 卸载逆序：QoL -> Invincibility -> BotOverlay -> Tooltip。
            QoLModule.Disable();
            InvincibilityModule.Disable();
            BotOverlayModule.Disable();
            TooltipModule.Disable();

            Logger?.LogInfo($"[{Name}] {Version} 已卸载。");
            Instance = null;
        }

        /// <summary>
        /// 软版本门禁：读取 EFT 可执行文件的构建号与目标比对，不匹配只记警告，
        /// 绝不抛异常、绝不禁用插件（调试工具必须在版本漂移时永远可加载）。
        /// 版本源：EscapeFromTarkov.exe 的 FileVersionInfo.FilePrivatePart。
        /// </summary>
        private static void WarnIfVersionMismatch()
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath);
                int build = versionInfo.FilePrivatePart;
                string fileVersion = versionInfo.FileVersion ?? string.Empty;

                if (build == TargetEftBuild)
                {
                    Logger.LogInfo(
                        $"[VersionGate] EFT 版本匹配：FileVersion='{fileVersion}' build={build}（目标 {TargetEftVersion}）。");
                }
                else if (build == 0)
                {
                    Logger.LogInfo(
                        $"[VersionGate] 无法读取 EFT 构建号（FileVersion='{fileVersion}'），跳过版本门禁（不阻止加载）。");
                }
                else
                {
                    Logger.LogWarning(
                        $"[VersionGate] EFT 版本不匹配：检测到 build={build}（FileVersion='{fileVersion}'），" +
                        $"目标 {TargetEftVersion}。继续加载；如遇补丁异常请核对版本。");
                }
            }
            catch (Exception ex)
            {
                // fail-open：门禁自身出错也绝不打断加载。
                Logger.LogInfo($"[VersionGate] 版本门禁读取失败，已忽略：{ex.Message}");
            }
        }
    }
}

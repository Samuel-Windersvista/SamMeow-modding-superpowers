using System;
using System.Diagnostics;
using BepInEx.Logging;

namespace DoorBreach
{
    /// <summary>
    /// 软版本门禁：读取 EFT 可执行文件的构建号与目标比对。
    /// </summary>
    /// <remarks>
    /// 不匹配只记警告，绝不抛异常、绝不禁用插件（fail-open）：补丁目标在版本漂移时可能失效，
    /// 但插件必须始终可加载，便于排查。版本源：EscapeFromTarkov.exe 的
    /// FileVersionInfo.FilePrivatePart。写法对齐包内既定形态（SPT41-DebugToolkit 的
    /// WarnIfVersionMismatch）。
    /// </remarks>
    internal static class VersionGate
    {
        /// <summary>目标 EFT 客户端版本（SPT 4.1.6 基线）。</summary>
        internal const string TargetEftVersion = "0.16.9.40743";

        /// <summary>目标 EFT 构建号。</summary>
        internal const int TargetEftBuild = 40743;

        /// <summary>执行门禁检查；任何异常都只记日志，不阻止加载。</summary>
        internal static void Check(ManualLogSource logger)
        {
            try
            {
                FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath);
                int build = versionInfo.FilePrivatePart;
                string fileVersion = versionInfo.FileVersion ?? string.Empty;

                if (build == TargetEftBuild)
                {
                    logger.LogInfo(
                        $"[VersionGate] EFT 版本匹配：FileVersion='{fileVersion}' build={build}（目标 {TargetEftVersion}）。");
                }
                else if (build == 0)
                {
                    logger.LogInfo(
                        $"[VersionGate] 无法读取 EFT 构建号（FileVersion='{fileVersion}'），跳过版本门禁（不阻止加载）。");
                }
                else
                {
                    logger.LogWarning(
                        $"[VersionGate] EFT 版本不匹配：检测到 build={build}（FileVersion='{fileVersion}'），" +
                        $"目标 {TargetEftVersion}。继续加载；如遇补丁异常请核对版本。");
                }
            }
            catch (Exception ex)
            {
                // fail-open：门禁自身出错也绝不打断加载。
                logger.LogInfo($"[VersionGate] 版本门禁读取失败，已忽略：{ex.Message}");
            }
        }
    }
}

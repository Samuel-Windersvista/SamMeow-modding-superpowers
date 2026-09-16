using BepInEx.Configuration;
using BepInEx.Logging;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 桥插件配置（STD-CFG-006）。
/// 客户端配置必须经 BepInEx Config.Bind 声明，落在
/// BepInEx/config/com.sammeow.tarkov-runtime-bridge.cfg；不自建 JSON 读取。
/// </summary>
// STD-CFG-006
internal sealed class BridgeConfig
{
    internal BridgeConfig(ConfigFile config)
    {
        Port = config.Bind(
            "Network",
            "Port",
            49777,
            "HTTP listen port. The bridge binds 127.0.0.1 only and is read-only.");

        SampleIntervalMs = config.Bind(
            "Sampling",
            "SampleIntervalMs",
            1000,
            new ConfigDescription(
                "Player position sampling interval in milliseconds.",
                new AcceptableValueRange<int>(250, 5_000)));

        LogWatchEnabled = config.Bind(
            "LogWatch",
            "Enabled",
            true,
            "Capture BepInEx log events into the in-memory ring buffer exposed by GET /logs/recent.");

        LogWatchMinLevel = config.Bind(
            "LogWatch",
            "MinLevel",
            LogLevel.Warning,
            "Minimum captured log level. Warning (default) captures Warning/Error/Fatal; " +
            "Message/Info/Debug are only captured when explicitly selected.");

        LogWatchRingSize = config.Bind(
            "LogWatch",
            "RingSize",
            1000,
            new ConfigDescription(
                "Number of captured log entries kept in memory (oldest entries are evicted first).",
                new AcceptableValueRange<int>(16, 10_000)));
    }

    /// <summary>HTTP 监听端口（仅 127.0.0.1）。默认 49777。</summary>
    internal ConfigEntry<int> Port { get; }

    /// <summary>主线程采样间隔（毫秒）。默认 1000。</summary>
    internal ConfigEntry<int> SampleIntervalMs { get; }

    /// <summary>是否注册日志监听器。默认 true。</summary>
    internal ConfigEntry<bool> LogWatchEnabled { get; }

    /// <summary>日志采集最小级别。默认 Warning（含 Error / Fatal）。</summary>
    internal ConfigEntry<LogLevel> LogWatchMinLevel { get; }

    /// <summary>日志环形缓冲容量（条）。默认 1000。</summary>
    internal ConfigEntry<int> LogWatchRingSize { get; }
}

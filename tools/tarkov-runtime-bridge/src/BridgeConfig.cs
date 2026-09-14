using BepInEx.Configuration;

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
    }

    /// <summary>HTTP 监听端口（仅 127.0.0.1）。默认 49777。</summary>
    internal ConfigEntry<int> Port { get; }

    /// <summary>主线程采样间隔（毫秒）。默认 1000。</summary>
    internal ConfigEntry<int> SampleIntervalMs { get; }
}

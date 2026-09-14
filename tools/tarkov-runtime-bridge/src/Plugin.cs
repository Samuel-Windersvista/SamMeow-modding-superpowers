using BepInEx;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// Tarkov Runtime Bridge 插件入口（tarkov-runtime-MCP Phase 2）。
/// 职责：建配置 -> 建持久 GameObject 并挂 <see cref="PositionSampler"/> 采样玩家/raid/bot 域
/// -> 启动仅绑 127.0.0.1 的 <see cref="HttpBridgeServer"/> 暴露只读端点
/// /bridge/info、/raid/player、/raid/status、/raid/bots。
///
/// STD-CLI-001：5.0（IL2CPP / BepInEx 6）继承 BepInEx.Unity.IL2CPP.BasePlugin，入口 Load()；
///              撤销路径为 Unload()（BepInEx 6 IL2CPP BasePlugin 无 IDisposable.Dispose()）。
/// STD-CLI-002：GUID 用反向域名记法（com.&lt;author&gt;.&lt;mod&gt;），全局唯一。
/// STD-META-005：版本 semver 三段式（0.1.0），与 csproj &lt;Version&gt; 一致。
/// STD-META-006：BepInPlugin 三参数齐备（GUID、显示名、版本）。
/// STD-LOG-003：客户端日志用 BepInEx 日志源（BasePlugin.Log）。
/// 部署形态：MO2 overlay，mod 根 = 游戏根，DLL 落在 BepInEx/plugins/。
/// </summary>
// STD-CLI-001 / STD-CLI-002 / STD-META-005 / STD-META-006
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    internal const string PluginGuid = "com.sammeow.tarkov-runtime-bridge";
    internal const string PluginName = "Tarkov Runtime Bridge";
    internal const string PluginVersion = "0.1.0";

    /// <summary>
    /// 桥 HTTP 协议版本（单点定义，MCP 握手校验用）。
    /// /bridge/info 与启动日志均引用此常量；破坏性契约变更时递增。
    /// </summary>
    internal const int ProtocolVersion = 1;

    private RaidStateStore store;
    private HttpBridgeServer httpServer;
    private GameObject samplerObject;

    public override void Load()
    {
        // STD-CFG-006：客户端配置经 BepInEx Config.Bind 声明（见 BridgeConfig.cs），
        // 运行时落在 BepInEx/config/com.sammeow.tarkov-runtime-bridge.cfg。
        var config = new BridgeConfig(Config);
        var port = config.Port.Value;
        var sampleIntervalMs = config.SampleIntervalMs.Value;

        store = new RaidStateStore();

        // IL2CPP 下注入的 MonoBehaviour 类型必须先注册，再挂到 GameObject 上。
        ClassInjector.RegisterTypeInIl2Cpp<PositionSampler>();
        samplerObject = new GameObject("TarkovRuntimeBridge.PositionSampler")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        Object.DontDestroyOnLoad(samplerObject);
        var sampler = samplerObject.AddComponent<PositionSampler>();
        sampler.Initialize(store, sampleIntervalMs, Log);

        // 启动失败（端口占用 / URL ACL 拒绝等）只记 error，不影响游戏运行。
        httpServer = new HttpBridgeServer(store, Log, port, sampleIntervalMs);
        if (httpServer.Start())
        {
            Log.LogInfo(
                $"{PluginName} {PluginVersion} (protocol {ProtocolVersion}) listening on " +
                $"http://127.0.0.1:{port}/bridge/info; sampling interval {sampleIntervalMs}ms.");
        }
        else
        {
            Log.LogError(
                $"{PluginName} {PluginVersion} HTTP endpoint unavailable on 127.0.0.1:{port} " +
                $"(protocol {ProtocolVersion}, sampling interval {sampleIntervalMs}ms); " +
                "sampling continues without HTTP.");
        }

        Log.LogInfo($"{PluginName} {PluginVersion} loaded.");
    }

    /// <summary>
    /// BepInEx 6 IL2CPP 的撤销路径（对应 4.1.5 的 OnDestroy）。
    /// 停 HTTP 监听并销毁采样 GameObject。
    /// </summary>
    public override bool Unload()
    {
        httpServer?.Dispose();
        httpServer = null;

        if (samplerObject != null)
        {
            Object.Destroy(samplerObject);
            samplerObject = null;
        }

        Log.LogInfo($"{PluginName} unloaded.");
        return true;
    }
}

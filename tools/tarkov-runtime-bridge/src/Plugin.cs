using System;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// Tarkov Runtime Bridge 插件入口（tarkov-runtime-MCP Phase 2）。
/// 职责：建配置 -> 建持久 GameObject 并挂 <see cref="PositionSampler"/> 采样玩家/raid/bot 域
/// 与事件域（<see cref="RaidEventCollector"/>）-> 启动仅绑 127.0.0.1 的
/// <see cref="HttpBridgeServer"/> 暴露只读端点
/// /bridge/info、/raid/player、/raid/status、/raid/bots、/raid/events、/logs/recent。
///
/// STD-CLI-001：5.0（IL2CPP / BepInEx 6）继承 BepInEx.Unity.IL2CPP.BasePlugin，入口 Load()；
///              撤销路径为 Unload()（BepInEx 6 IL2CPP BasePlugin 无 IDisposable.Dispose()）。
/// STD-CLI-002：GUID 用反向域名记法（com.&lt;author&gt;.&lt;mod&gt;），全局唯一。
/// STD-CLI-003：撤离事件 patch <c>LocalGame.Stop</c>（<see cref="LocalGameStopPatch"/>）
///              + 受伤事件 patch <c>ActiveHealthController.ApplyDamage</c>
///              （<see cref="ApplyDamagePatch"/>），均为显式 [HarmonyPatch(typeof(...))]。
/// STD-CLI-007：Harmony 生命周期在此管理（new Harmony + PatchAll + Unload 撤销）。
/// STD-META-005：版本 semver 三段式，与 csproj &lt;Version&gt; 一致。
/// STD-META-006：BepInPlugin 三参数齐备（GUID、显示名、版本）。
/// STD-LOG-003：客户端日志用 BepInEx 日志源（BasePlugin.Log）。
/// 部署形态：MO2 overlay，mod 根 = 游戏根，DLL 落在 BepInEx/plugins/。
/// </summary>
// STD-CLI-001 / STD-CLI-002 / STD-CLI-003 / STD-CLI-007 / STD-META-005 / STD-META-006
[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    internal const string PluginGuid = "com.sammeow.tarkov-runtime-bridge";
    internal const string PluginName = "Tarkov Runtime Bridge";
    internal const string PluginVersion = "0.2.0";

    /// <summary>
    /// 桥 HTTP 协议版本（单点定义，MCP 握手校验用）。
    /// /bridge/info 与启动日志均引用此常量；破坏性契约变更时递增。
    /// </summary>
    internal const int ProtocolVersion = 1;

    private RaidStateStore store;
    private RaidEventBuffer events;
    private LogRingBuffer logs;
    private LogSummaryStore summaries;
    private LogWatchListener logListener;
    private RaidEventCollector collector;
    private HttpBridgeServer httpServer;
    private GameObject samplerObject;
    private Harmony harmony;

    public override void Load()
    {
        // STD-CFG-006：客户端配置经 BepInEx Config.Bind 声明（见 BridgeConfig.cs），
        // 运行时落在 BepInEx/config/com.sammeow.tarkov-runtime-bridge.cfg。
        var config = new BridgeConfig(Config);
        var port = config.Port.Value;
        var sampleIntervalMs = config.SampleIntervalMs.Value;

        store = new RaidStateStore();
        events = new RaidEventBuffer();
        logs = new LogRingBuffer(config.LogWatchRingSize.Value);
        summaries = new LogSummaryStore();
        collector = new RaidEventCollector(events, Log);

        // 日志捕获：仅在启用时注册监听器（注册即全局生效；阈值低于 Info 会捕获大量
        // Unity 转发日志，故默认 Warning）。监听器回调只写内存，见 LogWatchListener。
        if (config.LogWatchEnabled.Value)
        {
            logListener = new LogWatchListener(logs, summaries, config.LogWatchMinLevel.Value);
            BepInEx.Logging.Logger.Listeners.Add(logListener);
        }

        // 撤离事件依赖 LocalGame.Stop 补丁；补丁失败不影响其余端点，仅记 error。
        TryApplyHarmonyPatches();

        // IL2CPP 下注入的 MonoBehaviour 类型必须先注册，再挂到 GameObject 上。
        ClassInjector.RegisterTypeInIl2Cpp<PositionSampler>();
        samplerObject = new GameObject("TarkovRuntimeBridge.PositionSampler")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        Object.DontDestroyOnLoad(samplerObject);
        var sampler = samplerObject.AddComponent<PositionSampler>();
        sampler.Initialize(store, collector, sampleIntervalMs, Log);

        // 启动失败（端口占用 / URL ACL 拒绝等）只记 error，不影响游戏运行。
        httpServer = new HttpBridgeServer(store, events, logs, summaries, Log, port, sampleIntervalMs);
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

        Log.LogInfo(
            $"{PluginName} {PluginVersion} loaded" +
            (logListener != null
                ? $" (log watch: min level {config.LogWatchMinLevel.Value}, ring {logs.Capacity})."
                : " (log watch disabled)."));
    }

    /// <summary>
    /// BepInEx 6 IL2CPP 的撤销路径（对应 4.1.5 的 OnDestroy）。
    /// 解除日志监听注册、撤销 Harmony 补丁、解除事件订阅、停 HTTP 监听并销毁采样 GameObject。
    /// </summary>
    public override bool Unload()
    {
        // 先摘监听器：本方法后续的收尾日志不应再进入（即将失效的）日志缓冲。
        if (logListener != null)
        {
            try
            {
                BepInEx.Logging.Logger.Listeners.Remove(logListener);
            }
            catch (Exception exception)
            {
                Log.LogWarning($"Log listener remove failed: {exception.Message}");
            }

            logListener.Dispose();
            logListener = null;
        }

        logs = null;
        summaries = null;

        if (harmony != null)
        {
            try
            {
                harmony.UnpatchSelf();
            }
            catch (Exception exception)
            {
                Log.LogWarning($"Harmony unpatch failed: {exception.Message}");
            }

            harmony = null;
        }

        collector?.Dispose();
        collector = null;
        events = null;

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

    private void TryApplyHarmonyPatches()
    {
        try
        {
            harmony = new Harmony(PluginGuid);
        }
        catch (Exception exception)
        {
            harmony = null;
            Log.LogError(
                $"Harmony init failed; extraction and damage events disabled: {exception.Message}");
            return;
        }

        // 逐补丁类独立应用：一类失败不影响另一类（例如 ApplyDamage 签名漂移不应停掉撤离采集）。
        var extractionApplied = TryApplyPatch(typeof(LocalGameStopPatch), "LocalGame.Stop extraction");
        var damageApplied = TryApplyPatch(typeof(ApplyDamagePatch), "ActiveHealthController.ApplyDamage damage");

        if (extractionApplied && damageApplied)
        {
            Log.LogInfo(
                "Harmony patches applied (LocalGame.Stop extraction + " +
                "ActiveHealthController.ApplyDamage hooks).");
        }
    }

    /// <summary>
    /// 应用单个补丁类；失败只记该类的 error，返回是否成功。
    /// HarmonyX 的 <c>PatchAll(Type)</c> 等价于 <c>CreateClassProcessor(type).Patch()</c>
    /// （已用参考程序集 IL 确认），故按类调用即可实现逐类隔离。
    /// </summary>
    private bool TryApplyPatch(Type patchType, string label)
    {
        try
        {
            harmony.PatchAll(patchType);
            return true;
        }
        catch (Exception exception)
        {
            Log.LogError(
                $"Harmony patch failed ({label}); corresponding events disabled: {exception.Message}");
            return false;
        }
    }
}

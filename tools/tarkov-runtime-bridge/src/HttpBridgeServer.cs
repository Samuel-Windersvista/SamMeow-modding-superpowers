using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BepInEx.Logging;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 只读 HTTP 桥：仅绑 127.0.0.1，暴露 GET /bridge/info、/raid/player、/raid/status、/raid/bots、/raid/events、/logs/recent、/logs/summary。
///
/// 契约（字段顺序稳定，可逐字节 diff）：
///   /bridge/info：200 {"pluginVersion":..,"protocolVersion":1,"capabilities":{...},"sampling":{"intervalMs":..},"network":{"host":"127.0.0.1","port":..}}
///   /raid/player：在 raid 200 {"inRaid":true,"position":{..},"rotation":{..},"pose":..,"health":{..},"weapon":..,"equipment":[..],"sampleAgeMs":..}；不在 raid 200 {"inRaid":false}
///   /raid/status：在 raid 200 {"inRaid":true,"map":..,"status":..,"remainingSeconds":..,"raidId":..,"sampleAgeMs":..}；不在 raid 200 {"inRaid":false}
///   /raid/bots：在 raid 200 摘要（total/alive/byCategory/spawner/sampleAgeMs）；?detail=1 另含 bots 明细（上限 200）与 truncated；不在 raid 200 {"inRaid":false}
///   /raid/events：200 {"inRaid":..,"seq":..,"dropped":..,"events":[..]}（?since=&amp;limit= 增量；缓冲跨 raid 保留，不在 raid 也返回历史）
///   /logs/recent：200 {"seq":..,"dropped":..,"entries":[{"seq":..,"ts":..,"level":..,"source":..,"text":..}]}（?level=&amp;since=&amp;limit= 增量；与 raid 状态无关）
///   /logs/summary：200 {"groups":[{"key":..,"level":..,"source":..,"count":..,"firstTs":..,"lastTs":..,"sampleText":..}],"overflowDropped":..}（?since= 时间游标；与 raid 状态无关）
///   未知路径：404 {"error":"not_found"}
///   其他方法：405 {"error":"method_not_allowed"}
///
/// 结构照抄 external/references/bepinex-mcp 的 HttpBridgeServer（HttpListener +
/// AcceptLoopAsync + HandleContextAsync）；启动失败只记 error，不向游戏抛出。
/// </summary>
// STD-LOG-003：日志走注入的 BepInEx ManualLogSource。
internal sealed class HttpBridgeServer : IDisposable
{
    private const string JsonContentType = "application/json; charset=utf-8";

    private readonly RaidStateStore store;
    private readonly RaidEventBuffer events;
    private readonly LogRingBuffer logs;
    private readonly LogSummaryStore summaries;
    private readonly ManualLogSource log;
    private readonly int port;
    private readonly int sampleIntervalMs;
    private readonly HttpListener listener = new HttpListener();
    private readonly CancellationTokenSource shutdown = new CancellationTokenSource();
    private Task serverTask;
    private bool listening;

    internal HttpBridgeServer(
        RaidStateStore store,
        RaidEventBuffer events,
        LogRingBuffer logs,
        LogSummaryStore summaries,
        ManualLogSource log,
        int port,
        int sampleIntervalMs)
    {
        this.store = store;
        this.events = events;
        this.logs = logs;
        this.summaries = summaries;
        this.log = log;
        this.port = port;
        this.sampleIntervalMs = sampleIntervalMs;
    }

    /// <summary>
    /// 尝试启动监听。端口占用 / URL ACL 拒绝等失败返回 false 并记 error，
    /// 绝不抛出（游戏不崩）。错误信息含端口与原因。
    /// </summary>
    internal bool Start()
    {
        if (listening)
        {
            return true;
        }

        if (port < 1 || port > 65_535)
        {
            log.LogError($"Tarkov Runtime Bridge: invalid HTTP port {port}; endpoint disabled.");
            return false;
        }

        try
        {
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            listener.Start();
        }
        catch (Exception exception)
        {
            log.LogError(
                $"Tarkov Runtime Bridge: failed to start HTTP listener on 127.0.0.1:{port} " +
                $"(port may be in use or URL ACL denied): {exception.Message}");
            SafeClose();
            return false;
        }

        listening = true;
        serverTask = Task.Run(() => AcceptLoopAsync(shutdown.Token));
        log.LogInfo($"Tarkov Runtime Bridge: listening on http://127.0.0.1:{port}{BridgeRouter.InfoPath}");
        return true;
    }

    internal void Stop()
    {
        if (!listening && serverTask == null)
        {
            return;
        }

        shutdown.Cancel();

        try
        {
            if (listener.IsListening)
            {
                listener.Stop();
            }
        }
        catch (Exception exception)
        {
            log.LogDebug($"Tarkov Runtime Bridge: listener stop failed: {exception.Message}");
        }

        SafeClose();

        if (serverTask != null)
        {
            try
            {
                serverTask.Wait(TimeSpan.FromSeconds(3));
            }
            catch (AggregateException exception)
            {
                // 关停竞态（accept loop 已被取消）属预期路径；Debug 留痕，不静默吞（STD-LOG-004）。
                log.LogDebug($"Tarkov Runtime Bridge: accept loop shutdown (expected): {exception.Message}");
            }
            catch (Exception exception)
            {
                log.LogDebug($"Tarkov Runtime Bridge: accept loop shutdown: {exception.Message}");
            }

            serverTask = null;
        }

        listening = false;
    }

    public void Dispose()
    {
        Stop();
        shutdown.Dispose();
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && listener.IsListening)
            {
                HttpListenerContext context;

                try
                {
                    context = await listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (InvalidOperationException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                _ = HandleContextAsync(context, cancellationToken);
            }
        }
        catch (Exception exception)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                log.LogError($"Tarkov Runtime Bridge: accept loop failed: {exception}");
            }
        }
    }

    private async Task HandleContextAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        int status;
        string body;

        try
        {
            var path = context.Request.Url?.AbsolutePath ?? string.Empty;
            var route = BridgeRouter.Resolve(path, context.Request.HttpMethod);
            status = BridgeRouter.StatusCodeFor(route);
            body = BuildBody(route, context.Request);
        }
        catch (Exception exception)
        {
            log.LogError($"Tarkov Runtime Bridge: request handling failed: {exception}");
            status = 500;
            body = BridgePayloads.InternalErrorBody;
        }

        try
        {
            var buffer = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = status;
            context.Response.ContentType = JsonContentType;
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream
                .WriteAsync(buffer, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is HttpListenerException or IOException or OperationCanceledException)
        {
            log.LogDebug($"Tarkov Runtime Bridge: client disconnected: {exception.Message}");
        }
        finally
        {
            context.Response.OutputStream.Close();
            context.Response.Close();
        }
    }

    /// <summary>
    /// 按路由结果构造响应体。路由判定见 <see cref="BridgeRouter"/>，
    /// 负载构造全部委托给纯逻辑 <see cref="BridgePayloads"/>（可独立单测）。
    /// </summary>
    private string BuildBody(BridgeRouteKind route, HttpListenerRequest request)
    {
        switch (route)
        {
            case BridgeRouteKind.NotFound:
                return BridgePayloads.NotFoundBody;
            case BridgeRouteKind.MethodNotAllowed:
                return BridgePayloads.MethodNotAllowedBody;
            case BridgeRouteKind.Info:
                return BridgePayloads.BuildInfo(
                    Plugin.PluginVersion,
                    Plugin.ProtocolVersion,
                    sampleIntervalMs,
                    port);
            case BridgeRouteKind.Player:
                return store.TryGet(out var playerState)
                    ? BridgePayloads.BuildPlayer(playerState, Environment.TickCount64)
                    : BridgePayloads.NotInRaidBody;
            case BridgeRouteKind.Status:
                return store.TryGet(out var raidState)
                    ? BridgePayloads.BuildStatus(raidState, Environment.TickCount64)
                    : BridgePayloads.NotInRaidBody;
            case BridgeRouteKind.Bots:
                return store.TryGet(out var botState)
                    ? BridgePayloads.BuildBots(
                        botState,
                        BridgeRouter.IsDetailRequested(request.QueryString["detail"]),
                        Environment.TickCount64)
                    : BridgePayloads.NotInRaidBody;
            case BridgeRouteKind.Events:
                return BridgePayloads.BuildEvents(
                    events,
                    RaidEventsQuery.ParseSince(request.QueryString["since"]),
                    RaidEventsQuery.ParseLimit(request.QueryString["limit"]),
                    store.TryGet(out _));
            case BridgeRouteKind.LogsRecent:
                // 与 raid 状态无关：即使不在 raid 也返回已捕获的日志。
                return BridgePayloads.BuildLogs(
                    logs,
                    RaidEventsQuery.ParseSince(request.QueryString["since"]),
                    RaidEventsQuery.ParseLimit(request.QueryString["limit"]),
                    LogWatchLevel.ParseMinLevel(request.QueryString["level"]));
            case BridgeRouteKind.LogsSummary:
                // 与 raid 状态无关；since 为时间游标（lastTs 晚于它才返回）。
                return BridgePayloads.BuildLogSummary(
                    summaries,
                    LogSummaryQuery.ParseSince(request.QueryString["since"]));
            default:
                return BridgePayloads.InternalErrorBody;
        }
    }

    private void SafeClose()
    {
        try
        {
            listener.Close();
        }
        catch (Exception exception)
        {
            log.LogDebug($"Tarkov Runtime Bridge: listener close failed: {exception.Message}");
        }
    }
}

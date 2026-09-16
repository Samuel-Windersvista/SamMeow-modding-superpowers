using System;
using BepInEx.Logging;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// BepInEx 日志监听器（游戏耦合薄适配层）：把达到阈值的日志写入
/// <see cref="LogRingBuffer"/>（原始条目增量流）与 <see cref="LogSummaryStore"/>
/// （归一化聚合视图）两条**并列**写路径。仅托管接口（<see cref="ILogListener"/>），
/// 零 IL2CPP 封送风险。
///
/// 红线（补丁体同款约束）：
/// 1. 回调只写内存——零 I/O、无阻塞；
/// 2. 绝不向游戏日志调用方外抛异常（异常会穿透到 Logger.Log* 的调用栈）；
/// 3. 空 catch 内绝不记录——本类是日志管线本身，记录会递归触发 LogEvent。
/// </summary>
internal sealed class LogWatchListener : ILogListener
{
    private readonly LogRingBuffer buffer;

    private readonly LogSummaryStore summary;

    /// <summary>归一化后的采集阈值；空表示不过滤（不推荐，默认 Warning）。</summary>
    private readonly string minLevel;

    /// <summary>交给 BepInEx 的预过滤掩码：低于阈值的级别根本不会回调到本类。</summary>
    private readonly LogLevel levelFilter;

    internal LogWatchListener(LogRingBuffer buffer, LogSummaryStore summary, LogLevel minLevel)
    {
        this.buffer = buffer;
        this.summary = summary;
        this.minLevel = LevelName(minLevel);
        levelFilter = MaskFor(this.minLevel);
    }

    /// <summary>BepInEx 6 的 <c>ILogListener.LogLevelFilter</c>（接口成员无默认实现，必须显式实现）。</summary>
    public LogLevel LogLevelFilter
    {
        get { return levelFilter; }
    }

    public void LogEvent(object sender, LogEventArgs eventArgs)
    {
        try
        {
            if (eventArgs == null)
            {
                return;
            }

            var level = LevelName(eventArgs.Level);
            if (!LogWatchLevel.IsAtLeast(level, minLevel))
            {
                return;
            }

            var source = eventArgs.Source != null ? eventArgs.Source.SourceName : string.Empty;
            var data = eventArgs.Data;
            var text = data != null ? data.ToString() : string.Empty;
            var ticks = DateTime.UtcNow.Ticks;

            // 两条并列写路径（各自独立加锁，绝不嵌套）：原始条目 + 归一化聚合。
            buffer.Append(ticks, level, source, text);
            summary.Observe(ticks, level, source, text);
        }
        catch
        {
            // 红线：吞掉一切异常（含 OOM 之外的意外）；此处绝不记录、绝不外抛。
        }
    }

    public void Dispose()
    {
        // 无托管资源持有：缓冲由 Plugin 拥有，解除注册由 Plugin 负责。
    }

    /// <summary>
    /// BepInEx <c>LogLevel</c>（[Flags]）→ 归一化级别名。
    /// 组合标志按最高严重度取名（与 <see cref="LogWatchLevel.Rank"/> 的排序一致）。
    /// </summary>
    internal static string LevelName(LogLevel level)
    {
        if ((level & LogLevel.Fatal) != 0)
        {
            return LogWatchLevel.Fatal;
        }

        if ((level & LogLevel.Error) != 0)
        {
            return LogWatchLevel.Error;
        }

        if ((level & LogLevel.Warning) != 0)
        {
            return LogWatchLevel.Warning;
        }

        if ((level & LogLevel.Message) != 0)
        {
            return LogWatchLevel.Message;
        }

        if ((level & LogLevel.Info) != 0)
        {
            return LogWatchLevel.Info;
        }

        if ((level & LogLevel.Debug) != 0)
        {
            return LogWatchLevel.Debug;
        }

        // None 及未知组合：用不可能达到任何有效阈值的级别名（"none" 归一化为空 → Rank=MaxValue）。
        return "none";
    }

    /// <summary>阈值 → 预过滤掩码：Fatal|Error|Warning 即「Warning 及以上」。</summary>
    private static LogLevel MaskFor(string minLevelName)
    {
        switch (minLevelName)
        {
            case LogWatchLevel.Fatal:
                return LogLevel.Fatal;
            case LogWatchLevel.Error:
                return LogLevel.Fatal | LogLevel.Error;
            case LogWatchLevel.Warning:
                return LogLevel.Fatal | LogLevel.Error | LogLevel.Warning;
            case LogWatchLevel.Message:
                return LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message;
            case LogWatchLevel.Info:
                return LogLevel.Fatal | LogLevel.Error | LogLevel.Warning | LogLevel.Message | LogLevel.Info;
            case LogWatchLevel.Debug:
                return LogLevel.All;
            default:
                // 未知 / 空阈值：不过滤，交给 IsAtLeast 兜底（IsAtLeast 对空阈值恒 true）。
                return LogLevel.All;
        }
    }
}

using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 单条捕获的日志（不可变）。<see cref="Seq"/> 由 <see cref="LogRingBuffer"/> 单调分配，
/// <see cref="TimestampUtcTicks"/> 为捕获时的墙钟 UTC Ticks，
/// <see cref="Level"/> 为 <see cref="LogWatchLevel"/> 归一化后的小写级别名（JSON 稳定字面量）。
/// </summary>
internal sealed class LogEntry
{
    internal LogEntry(long seq, long timestampUtcTicks, string level, string source, string text)
    {
        Seq = seq;
        TimestampUtcTicks = timestampUtcTicks;
        Level = level ?? string.Empty;
        Source = source ?? string.Empty;
        Text = text ?? string.Empty;
    }

    internal long Seq { get; }

    internal long TimestampUtcTicks { get; }

    /// <summary>归一化级别名：fatal / error / warning / message / info / debug（小写）。</summary>
    internal string Level { get; }

    /// <summary>日志来源（BepInEx <c>ILogSource.SourceName</c>，如 Unity / Assembly-CSharp）。</summary>
    internal string Source { get; }

    /// <summary>日志正文（零拷贝引用原始字符串）。</summary>
    internal string Text { get; }
}

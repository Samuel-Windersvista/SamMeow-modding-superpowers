using Microsoft.Extensions.Logging;
using Spectre.Console;
using SPTarkov.Common.Models.Logging;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>记录型 SPT 日志桩，供需要 ISptLogger&lt;T&gt; 的模块测试使用。</summary>
internal sealed class TestSptLogger<T> : ISptLogger<T>
{
    public List<string> InfoMessages { get; } = [];

    public List<string> WarningMessages { get; } = [];

    public List<string> ErrorMessages { get; } = [];

    public void Info(string data, Exception? ex = null) => InfoMessages.Add(data);

    public void Warning(string data, Exception? ex = null) => WarningMessages.Add(data);

    public void Error(string data, Exception? ex = null) => ErrorMessages.Add(data);

    public void Debug(string data, Exception? ex = null) { }

    public void Success(string data, Exception? ex = null) { }

    public void Critical(string data, Exception? ex = null) { }

    public void LogWithColor(string data, Color? textColor = null, Color? backgroundColor = null, Exception? ex = null) { }

    public void Log(
        LogLevel level,
        string data,
        Color? textColor = null,
        Color? backgroundColor = null,
        Exception? ex = null) { }

    public bool IsLogEnabled(LogLevel level) => true;
}

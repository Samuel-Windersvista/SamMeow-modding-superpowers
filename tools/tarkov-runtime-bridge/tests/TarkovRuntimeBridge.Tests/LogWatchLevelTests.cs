using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class LogWatchLevelTests
{
    [Theory]
    [InlineData("fatal", "fatal")]
    [InlineData("Fatal", "fatal")]
    [InlineData("ERROR", "error")]
    [InlineData("Error", "error")]
    [InlineData("warning", "warning")]
    [InlineData("WARNING", "warning")]
    [InlineData("Message", "message")]
    [InlineData("info", "info")]
    [InlineData("DEBUG", "debug")]
    [InlineData("  warning  ", "warning")]
    [InlineData("none", "")]
    [InlineData("bogus", "")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Normalize_is_case_insensitive_and_lenient(string value, string expected)
    {
        Assert.Equal(expected, LogWatchLevel.Normalize(value));
    }

    [Theory]
    [InlineData("fatal", 0)]
    [InlineData("error", 1)]
    [InlineData("warning", 2)]
    [InlineData("message", 3)]
    [InlineData("info", 4)]
    [InlineData("debug", 5)]
    public void Rank_orders_by_severity_not_by_flags_value(string level, int expected)
    {
        // BepInEx LogLevel 是 [Flags]：Fatal=1 < Error=2 < Warning=4 < Message=8 < Info=16 < Debug=32。
        // 严重度顺序不能直接用数值比较。
        Assert.Equal(expected, LogWatchLevel.Rank(level));
    }

    [Fact]
    public void Rank_of_unknown_level_is_lowest_priority()
    {
        Assert.True(LogWatchLevel.Rank("bogus") > LogWatchLevel.Rank("debug"));
        Assert.True(LogWatchLevel.Rank(null) > LogWatchLevel.Rank("debug"));
    }

    [Theory]
    [InlineData("fatal", "warning", true)]
    [InlineData("error", "warning", true)]
    [InlineData("warning", "warning", true)]
    [InlineData("message", "warning", false)]
    [InlineData("info", "warning", false)]
    [InlineData("debug", "warning", false)]
    [InlineData("fatal", "error", true)]
    [InlineData("error", "error", true)]
    [InlineData("warning", "error", false)]
    [InlineData("fatal", "fatal", true)]
    [InlineData("error", "fatal", false)]
    [InlineData("debug", "debug", true)]
    [InlineData("info", "debug", true)]
    public void IsAtLeast_uses_severity_threshold(string level, string minLevel, bool expected)
    {
        Assert.Equal(expected, LogWatchLevel.IsAtLeast(level, minLevel));
    }

    [Theory]
    [InlineData("warning", "")]
    [InlineData("warning", null)]
    [InlineData("warning", "bogus")]
    [InlineData("debug", "")]
    public void IsAtLeast_without_effective_minimum_accepts_everything(string level, string minLevel)
    {
        Assert.True(LogWatchLevel.IsAtLeast(level, minLevel));
    }

    [Fact]
    public void IsAtLeast_rejects_unknown_captured_level()
    {
        Assert.False(LogWatchLevel.IsAtLeast("bogus", "warning"));
        Assert.False(LogWatchLevel.IsAtLeast(null, "warning"));
    }

    [Theory]
    [InlineData("WARNING", "warning")]
    [InlineData("error", "error")]
    [InlineData("fatal", "fatal")]
    [InlineData("", "")]
    [InlineData(null, "")]
    [InlineData("bogus", "")]
    public void Parse_min_level_filter_is_lenient(string value, string expected)
    {
        Assert.Equal(expected, LogWatchLevel.ParseMinLevel(value));
    }
}

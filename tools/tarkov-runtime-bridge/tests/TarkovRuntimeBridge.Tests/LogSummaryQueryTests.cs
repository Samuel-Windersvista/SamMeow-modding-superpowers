using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class LogSummaryQueryTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("0", 0)]
    [InlineData("-3", 0)]
    [InlineData("abc", 0)]
    [InlineData("2026-13-45T99:99:99Z", 0)]
    public void Parse_since_is_lenient(string value, long expected)
    {
        Assert.Equal(expected, LogSummaryQuery.ParseSince(value));
    }

    [Fact]
    public void Parse_since_accepts_raw_utc_ticks()
    {
        Assert.Equal(638_000_000_000_000_000L, LogSummaryQuery.ParseSince("638000000000000000"));
    }

    [Fact]
    public void Parse_since_accepts_iso_8601_as_emitted_by_the_endpoint()
    {
        // 往返契约：客户端可把 lastTs 原样回填给 since。
        var ticks = new System.DateTime(2026, 9, 16, 12, 34, 56, System.DateTimeKind.Utc).Ticks;
        var iso = BridgePayloads.FormatTimestamp(ticks);

        Assert.Equal(ticks, LogSummaryQuery.ParseSince(iso));
    }

    [Fact]
    public void Parse_since_accepts_iso_8601_with_explicit_offset()
    {
        var expected = new System.DateTime(2026, 9, 16, 12, 0, 0, System.DateTimeKind.Utc).Ticks;

        Assert.Equal(expected, LogSummaryQuery.ParseSince("2026-09-16T14:00:00+02:00"));
    }

    [Fact]
    public void Default_since_returns_everything()
    {
        Assert.Equal(0, LogSummaryQuery.DefaultSinceTicks);
    }
}

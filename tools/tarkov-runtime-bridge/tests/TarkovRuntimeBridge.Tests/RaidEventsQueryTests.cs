using Xunit;

namespace SamMeow.TarkovRuntimeBridge.Tests;

public class RaidEventsQueryTests
{
    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("0", 0)]
    [InlineData("-3", 0)]
    [InlineData("abc", 0)]
    [InlineData("5", 5)]
    [InlineData("1000", 1000)]
    public void Parse_since_is_lenient(string value, long expected)
    {
        Assert.Equal(expected, RaidEventsQuery.ParseSince(value));
    }

    [Fact]
    public void Parse_limit_uses_default_when_missing_or_invalid()
    {
        Assert.Equal(RaidEventBuffer.DefaultLimit, RaidEventsQuery.ParseLimit(null));
        Assert.Equal(RaidEventBuffer.DefaultLimit, RaidEventsQuery.ParseLimit(string.Empty));
        Assert.Equal(RaidEventBuffer.DefaultLimit, RaidEventsQuery.ParseLimit("abc"));
        Assert.Equal(RaidEventBuffer.DefaultLimit, RaidEventsQuery.ParseLimit("0"));
        Assert.Equal(RaidEventBuffer.DefaultLimit, RaidEventsQuery.ParseLimit("-1"));
    }

    [Fact]
    public void Parse_limit_honours_positive_values()
    {
        Assert.Equal(1, RaidEventsQuery.ParseLimit("1"));
        Assert.Equal(250, RaidEventsQuery.ParseLimit("250"));
    }

    [Fact]
    public void Parse_limit_clamps_to_buffer_capacity()
    {
        Assert.Equal(
            RaidEventBuffer.DefaultCapacity,
            RaidEventsQuery.ParseLimit("999999"));
    }
}

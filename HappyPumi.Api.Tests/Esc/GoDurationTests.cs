using System;
using HappyPumi.Api.Esc;

namespace HappyPumi.Api.Tests.Esc;

/// <summary>Unit tests for the Go-duration parser used by the ESC open session.</summary>
public sealed class GoDurationTests
{
    [Theory]
    [InlineData("2h", 2, 0, 0)]
    [InlineData("2h45m", 2, 45, 0)]
    [InlineData("30m", 0, 30, 0)]
    [InlineData("10s", 0, 0, 10)]
    public void ParsesCompoundDurations(string text, int h, int m, int s)
    {
        Assert.Equal(new TimeSpan(h, m, s), GoDuration.Parse(text, TimeSpan.Zero));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    public void FallsBackWhenMissingOrInvalid(string? text)
    {
        var fallback = TimeSpan.FromHours(1);
        Assert.Equal(fallback, GoDuration.Parse(text, fallback));
    }

    [Fact]
    public void ParsesMilliseconds()
    {
        Assert.Equal(TimeSpan.FromMilliseconds(300), GoDuration.Parse("300ms", TimeSpan.Zero));
    }
}

using Microsoft.Extensions.Time.Testing;
using NJobDesk.Core.Services;

namespace NJobDesk.Tests.Services;

public class CronosCronServiceTests
{
    private static CronosCronService Create() =>
        new(new FakeTimeProvider(new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero)));

    [Fact]
    public void Validates_standard_five_field_expression()
    {
        var result = Create().Validate("*/5 * * * *", nextFireTimeCount: 3);

        Assert.True(result.IsValid);
        Assert.Equal(3, result.NextFireTimesUtc.Count);
        Assert.Equal(new DateTime(2026, 8, 1, 12, 5, 0, DateTimeKind.Utc), result.NextFireTimesUtc[0]);
        Assert.NotNull(result.Summary);
    }

    [Fact]
    public void Validates_quartz_style_six_field_expression()
    {
        var result = Create().Validate("0 0/15 * * * ?", nextFireTimeCount: 2);

        Assert.True(result.IsValid);
        Assert.Equal(new DateTime(2026, 8, 1, 12, 15, 0, DateTimeKind.Utc), result.NextFireTimesUtc[0]);
    }

    [Fact]
    public void Rejects_seven_field_expression_with_year()
    {
        var result = Create().Validate("0 0 12 * * ? 2026");

        Assert.False(result.IsValid);
        Assert.Contains("year", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-cron")]
    public void Rejects_invalid_expressions(string expression)
    {
        var result = Create().Validate(expression);

        Assert.False(result.IsValid);
        Assert.NotNull(result.Error);
    }

    [Fact]
    public void Rejects_unknown_time_zone()
    {
        var result = Create().Validate("*/5 * * * *", timeZoneId: "Mars/Olympus_Mons");

        Assert.False(result.IsValid);
        Assert.Contains("time zone", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Clamps_next_fire_time_count()
    {
        var result = Create().Validate("* * * * *", nextFireTimeCount: 999);

        Assert.True(result.IsValid);
        Assert.Equal(ICronService.MaxNextFireTimeCount, result.NextFireTimesUtc.Count);
    }
}

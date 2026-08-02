namespace NJobDesk.Core.Models;

public record CronValidationRequestModel
{
    public required string CronExpression { get; init; }

    public int NextFireTimeCount { get; init; } = 5;

    public string? TimeZoneId { get; init; }
}

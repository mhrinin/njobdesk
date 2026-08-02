namespace NJobDesk.Core.Models;

public record RescheduleRequestModel
{
    public required string CronExpression { get; init; }

    public string? TimeZoneId { get; init; }
}

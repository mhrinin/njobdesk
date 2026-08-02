namespace NJobDesk.Core.Models;

public record TriggerModel
{
    public required string Id { get; init; }

    public string? Group { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public required TriggerType Type { get; init; }

    public string? CronExpression { get; init; }

    public string? CronSummary { get; init; }

    public string? TimeZoneId { get; init; }

    public required JobState State { get; init; }

    public DateTime? NextFireTimeUtc { get; init; }

    public DateTime? PreviousFireTimeUtc { get; init; }

    public DateTime StartTimeUtc { get; init; }

    public DateTime? EndTimeUtc { get; init; }

    public string? MisfireInstruction { get; init; }

    public int? Priority { get; init; }
}

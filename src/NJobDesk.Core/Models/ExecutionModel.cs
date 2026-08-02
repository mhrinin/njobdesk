using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Models;

public record ExecutionModel
{
    public long Id { get; init; }

    public required string JobGroup { get; init; }

    public required string JobName { get; init; }

    public required string TriggerGroup { get; init; }

    public required string TriggerName { get; init; }

    public DateTime StartedUtc { get; init; }

    public DateTime? FinishedUtc { get; init; }

    public long? DurationMs { get; init; }

    public required ExecutionStatus State { get; init; }

    public string? ErrorMessage { get; init; }

    public required string SchedulerInstanceId { get; init; }

    public bool Recovering { get; init; }
}

using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Models;

public record ExecutionModel
{
    public long Id { get; init; }

    public string ProviderKey { get; init; } = string.Empty;

    public required string JobId { get; init; }

    public required string JobName { get; init; }

    public string? JobGroup { get; init; }

    public string? TriggerName { get; init; }

    public DateTime StartedUtc { get; init; }

    public DateTime? FinishedUtc { get; init; }

    public long? DurationMs { get; init; }

    public required ExecutionStatus State { get; init; }

    public string? ErrorMessage { get; init; }

    public required string SchedulerInstanceId { get; init; }

    public bool Recovering { get; init; }
}

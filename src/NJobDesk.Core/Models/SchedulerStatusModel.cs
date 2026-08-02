namespace NJobDesk.Core.Models;

public record SchedulerStatusModel
{
    public required SchedulerState State { get; init; }

    public string? SchedulerName { get; init; }

    public string? SchedulerInstanceId { get; init; }

    public bool Clustered { get; init; }

    public bool HistoryEnabled { get; init; }

    public string? StoreType { get; init; }

    public int? ThreadPoolSize { get; init; }

    public DateTime? RunningSinceUtc { get; init; }
}

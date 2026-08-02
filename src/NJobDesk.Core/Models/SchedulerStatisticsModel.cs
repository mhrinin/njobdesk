namespace NJobDesk.Core.Models;

public record SchedulerStatisticsModel
{
    public int JobsTotal { get; init; }

    public int JobsPaused { get; init; }

    public int RunningCount { get; init; }

    public int Succeeded24h { get; init; }

    public int Failed24h { get; init; }

    public IReadOnlyList<ExecutionBucketModel> Buckets { get; init; } = [];
}

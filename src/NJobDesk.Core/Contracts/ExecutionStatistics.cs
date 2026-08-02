namespace NJobDesk.Core.Contracts;

public sealed record ExecutionStatistics
{
    public int RunningCount { get; init; }

    public int Succeeded24h { get; init; }

    public int Failed24h { get; init; }

    public IReadOnlyList<ExecutionBucket> Buckets { get; init; } = [];
}

public sealed record ExecutionBucket
{
    public DateTime HourStartUtc { get; init; }

    public int Succeeded { get; init; }

    public int Failed { get; init; }
}

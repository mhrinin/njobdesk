namespace NJobDesk.Core.Models;

public record ExecutionBucketModel
{
    public DateTime HourStartUtc { get; init; }

    public int Succeeded { get; init; }

    public int Failed { get; init; }
}

using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Models;

public record ExecutionLogModel
{
    public DateTime TimestampUtc { get; init; }

    public required ExecutionLogLevel Level { get; init; }

    public required string Category { get; init; }

    public required string Message { get; init; }

    public string? Exception { get; init; }

    public string? Properties { get; init; }
}

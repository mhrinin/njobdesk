namespace NJobDesk.Core.Entities;

public class JobExecutionLog
{
    public long Id { get; set; }

    public long ExecutionId { get; set; }

    public DateTime TimestampUtc { get; set; }

    public ExecutionLogLevel Level { get; set; }

    public required string Category { get; set; }

    public required string Message { get; set; }

    public string? Exception { get; set; }

    public string? Properties { get; set; }
}

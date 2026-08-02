namespace NJobDesk.Core.Entities;

public class JobExecutionHistory
{
    public long Id { get; set; }

    public required string FireInstanceId { get; set; }

    public required string SchedulerInstanceId { get; set; }

    public required string ProviderKey { get; set; }

    public required string JobId { get; set; }

    public required string JobName { get; set; }

    public string? JobGroup { get; set; }

    public string? TriggerId { get; set; }

    public string? TriggerName { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? FinishedUtc { get; set; }

    public long? DurationMs { get; set; }

    public ExecutionStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public bool Recovering { get; set; }
}

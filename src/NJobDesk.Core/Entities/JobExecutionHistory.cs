namespace NJobDesk.Core.Entities;

public class JobExecutionHistory
{
    public long Id { get; set; }

    public required string FireInstanceId { get; set; }

    public required string SchedulerInstanceId { get; set; }

    public required string JobGroup { get; set; }

    public required string JobName { get; set; }

    public required string TriggerGroup { get; set; }

    public required string TriggerName { get; set; }

    public DateTime StartedUtc { get; set; }

    public DateTime? FinishedUtc { get; set; }

    public long? DurationMs { get; set; }

    public ExecutionStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public bool Recovering { get; set; }
}

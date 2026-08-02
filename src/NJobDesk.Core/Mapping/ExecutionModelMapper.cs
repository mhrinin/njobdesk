using NJobDesk.Core.Entities;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Mapping;

/// <summary>Maps execution-history entities to API models. Provider-agnostic.</summary>
public static class ExecutionModelMapper
{
    /// <summary>Maps a persisted log entry to its API model.</summary>
    /// <param name="entry">The persisted log entry.</param>
    public static ExecutionLogModel MapExecutionLog(JobExecutionLog entry) => new()
    {
        TimestampUtc = DateTime.SpecifyKind(entry.TimestampUtc, DateTimeKind.Utc),
        Level = entry.Level,
        Category = entry.Category,
        Message = entry.Message,
        Exception = entry.Exception,
        Properties = entry.Properties,
    };

    /// <summary>Maps a persisted execution to its API model.</summary>
    /// <param name="entry">The persisted execution.</param>
    public static ExecutionModel MapExecution(JobExecutionHistory entry) => new()
    {
        Id = entry.Id,
        ProviderKey = entry.ProviderKey,
        JobId = entry.JobId,
        JobName = entry.JobName,
        JobGroup = entry.JobGroup,
        TriggerName = entry.TriggerName,
        StartedUtc = DateTime.SpecifyKind(entry.StartedUtc, DateTimeKind.Utc),
        FinishedUtc = entry.FinishedUtc is { } finishedUtc ? DateTime.SpecifyKind(finishedUtc, DateTimeKind.Utc) : null,
        DurationMs = entry.DurationMs,
        State = entry.Status,
        ErrorMessage = entry.ErrorMessage,
        SchedulerInstanceId = entry.SchedulerInstanceId,
        Recovering = entry.Recovering,
    };
}

using NJobDesk.Core.Entities;

namespace NJobDesk.History.EFCore.Capture;

/// <summary>
/// An active per-run log capture. Dispose when the run finishes, then hand the scope to
/// <see cref="IExecutionLogStore.SaveAsync"/> to persist what was captured.
/// </summary>
public sealed class ExecutionLogCaptureScope : IDisposable
{
    private readonly TimeProvider _timeProvider;

    internal ExecutionLogCaptureScope(ExecutionLogBuffer buffer, TimeProvider timeProvider)
    {
        Buffer = buffer;
        _timeProvider = timeProvider;
    }

    internal ExecutionLogBuffer Buffer { get; }

    /// <summary>Records the exception that ended the run as an error log entry.</summary>
    /// <param name="category">The log category, typically the job type name.</param>
    /// <param name="exception">The exception the run threw.</param>
    public void RecordException(string category, Exception exception) =>
        Buffer.Add(new JobExecutionLog
        {
            TimestampUtc = _timeProvider.GetUtcNow().UtcDateTime,
            Level = ExecutionLogLevel.Error,
            Category = category,
            Message = "Job execution threw an exception.",
            Exception = exception.ToString(),
        });

    public void Dispose()
    {
        if (ReferenceEquals(ExecutionLogBuffer.Current, Buffer))
        {
            ExecutionLogBuffer.Current = null;
        }
    }
}

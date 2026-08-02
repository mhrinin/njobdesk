namespace NJobDesk.History.EFCore.Capture;

/// <summary>
/// Per-run <c>ILogger</c> capture seam for scheduler providers. Open a scope before running a job
/// and dispose it afterwards; everything logged through the host's logging pipeline while the scope
/// is active (on the run's async flow) is buffered, and
/// <see cref="IExecutionLogStore.SaveAsync"/> attaches the buffer to the run's history entry.
/// </summary>
public interface IExecutionLogCapture
{
    /// <summary>Opens a capture scope, or returns <c>null</c> when log capture is disabled.</summary>
    ExecutionLogCaptureScope? BeginScope();
}

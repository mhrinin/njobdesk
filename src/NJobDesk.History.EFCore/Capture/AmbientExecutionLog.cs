using NJobDesk.Core.Entities;

namespace NJobDesk.History.EFCore.Capture;

/// <summary>
/// Lets alternative logging pipelines (e.g. a Serilog sink in hosts that replace the MEL logger
/// factory) feed entries into the active per-run capture scope on the current async flow.
/// </summary>
public static class AmbientExecutionLog
{
    /// <summary>Whether a capture scope is active on the current async flow.</summary>
    public static bool IsActive => ExecutionLogBuffer.Current is not null;

    /// <summary>Adds an entry to the active capture scope, if any.</summary>
    /// <param name="entry">The log entry.</param>
    public static bool TryAdd(JobExecutionLog entry)
    {
        if (ExecutionLogBuffer.Current is not { } buffer)
        {
            return false;
        }

        buffer.Add(entry);
        return true;
    }
}

using Microsoft.Extensions.Options;
using NJobDesk.History.EFCore.Configuration;

namespace NJobDesk.History.EFCore.Capture;

internal sealed class ExecutionLogCapture(
    IOptionsMonitor<NJobDeskHistoryOptions> options,
    TimeProvider timeProvider)
    : IExecutionLogCapture
{
    public ExecutionLogCaptureScope? BeginScope()
    {
        var logs = options.CurrentValue.Logs;
        if (!logs.Enabled)
        {
            return null;
        }

        var buffer = new ExecutionLogBuffer(logs.MaxEntriesPerRun);
        ExecutionLogBuffer.Current = buffer;
        return new ExecutionLogCaptureScope(buffer, timeProvider);
    }
}

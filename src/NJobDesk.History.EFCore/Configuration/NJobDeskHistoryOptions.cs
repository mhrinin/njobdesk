using NJobDesk.Core.Entities;

namespace NJobDesk.History.EFCore.Configuration;

public class NJobDeskHistoryOptions
{
    /// <summary>Finished executions older than this are deleted by the cleanup service.</summary>
    public int RetentionDays { get; set; } = 30;

    /// <summary>How often the retention cleanup runs.</summary>
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>
    /// When true, startup reconciliation only touches rows written by this node (matched on the
    /// scheduler instance id / machine name), leaving other nodes' running rows alone.
    /// </summary>
    public bool Clustered { get; set; }

    public LogCaptureOptions Logs { get; set; } = new();

    public class LogCaptureOptions
    {
        public bool Enabled { get; set; } = true;

        public ExecutionLogLevel MinimumLevel { get; set; } = ExecutionLogLevel.Information;

        public int MaxEntriesPerRun { get; set; } = 1000;
    }
}

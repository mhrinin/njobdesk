namespace NJobDesk.Core.Providers;

/// <summary>
/// Declares which dashboard features a scheduler provider supports. Capabilities are surfaced on the
/// status endpoint and per job so the client hides unsupported actions, and the aggregation layer
/// refuses management calls the provider did not opt into.
/// </summary>
public record SchedulerCapabilities
{
    /// <summary>Jobs can be triggered to run immediately.</summary>
    public bool TriggerNow { get; init; }

    /// <summary>Jobs and triggers can be paused and resumed (including error-state reset).</summary>
    public bool Pause { get; init; }

    /// <summary>Cron schedules can be edited from the dashboard.</summary>
    public bool ScheduleEditing { get; init; }

    /// <summary>Jobs can be deleted and triggers unscheduled.</summary>
    public bool Delete { get; init; }

    /// <summary>Jobs are organized in named groups.</summary>
    public bool Groups { get; init; }

    /// <summary>Jobs have real trigger entities (otherwise a single synthetic trigger is projected).</summary>
    public bool Triggers { get; init; }

    /// <summary>Finished executions are persisted and appear in the history views.</summary>
    public bool History { get; init; }

    /// <summary>Log entries captured during a run can be inspected per execution.</summary>
    public bool RunLogs { get; init; }

    /// <summary>Running executions can be interrupted.</summary>
    public bool Interrupt { get; init; }

    /// <summary>No capabilities; a read-only listing of jobs.</summary>
    public static readonly SchedulerCapabilities None = new();

    /// <summary>Every capability; full-featured schedulers such as Quartz.</summary>
    public static readonly SchedulerCapabilities Full = new()
    {
        TriggerNow = true,
        Pause = true,
        ScheduleEditing = true,
        Delete = true,
        Groups = true,
        Triggers = true,
        History = true,
        RunLogs = true,
        Interrupt = true,
    };
}

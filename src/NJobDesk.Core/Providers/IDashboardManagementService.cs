using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Providers;

/// <summary>
/// Write side of the dashboard across all registered providers. Ids are dashboard-level
/// (<see cref="CompositeId"/>); operations the target provider's capabilities do not allow are
/// refused without reaching the provider.
/// </summary>
public interface IDashboardManagementService
{
    Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> PauseJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> ResumeJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<bool> PauseTriggerAsync(string triggerId, CancellationToken cancellationToken = default);

    Task<bool> ResumeTriggerAsync(string triggerId, CancellationToken cancellationToken = default);

    Task<bool> ResetTriggerFromErrorAsync(string triggerId, CancellationToken cancellationToken = default);

    Task<bool> UnscheduleTriggerAsync(string triggerId, CancellationToken cancellationToken = default);

    Task PauseAllAsync(CancellationToken cancellationToken = default);

    Task ResumeAllAsync(CancellationToken cancellationToken = default);

    Task<RescheduleResult> RescheduleAsync(
        string triggerId,
        RescheduleRequestModel request,
        CancellationToken cancellationToken = default);
}

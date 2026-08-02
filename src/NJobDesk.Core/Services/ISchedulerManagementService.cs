using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

public interface ISchedulerManagementService
{
    Task<bool> TriggerJobAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<bool> PauseJobAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<bool> ResumeJobAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<bool> DeleteJobAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<bool> PauseTriggerAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<bool> ResumeTriggerAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<bool> ResetTriggerFromErrorAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<bool> UnscheduleTriggerAsync(string group, string name, CancellationToken cancellationToken = default);

    Task PauseAllAsync(CancellationToken cancellationToken = default);

    Task ResumeAllAsync(CancellationToken cancellationToken = default);

    Task<RescheduleResult> RescheduleAsync(
        string group,
        string name,
        RescheduleRequestModel request,
        CancellationToken cancellationToken = default);
}

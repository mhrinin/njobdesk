using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

/// <summary>
/// Write side of a scheduler provider. Job and trigger ids are provider-local (see
/// <see cref="Providers.ISchedulerProvider"/>). The dashboard layer only calls operations the
/// provider's declared capabilities allow.
/// </summary>
public interface ISchedulerManagementService
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

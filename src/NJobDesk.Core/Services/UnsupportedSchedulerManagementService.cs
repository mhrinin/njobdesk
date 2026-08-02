using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

/// <summary>
/// Management side for read-only providers: every operation reports failure. Providers whose
/// capabilities allow no management operations can expose this instead of writing their own —
/// the dashboard refuses the calls before they get here.
/// </summary>
public sealed class UnsupportedSchedulerManagementService : ISchedulerManagementService
{
    public Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> PauseJobAsync(string jobId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> ResumeJobAsync(string jobId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> PauseTriggerAsync(string triggerId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> ResumeTriggerAsync(string triggerId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> ResetTriggerFromErrorAsync(string triggerId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> UnscheduleTriggerAsync(string triggerId, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task PauseAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ResumeAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<RescheduleResult> RescheduleAsync(
        string triggerId,
        RescheduleRequestModel request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RescheduleResult(
            RescheduleStatus.NotSupported, "This scheduler provider does not support schedule editing."));
}

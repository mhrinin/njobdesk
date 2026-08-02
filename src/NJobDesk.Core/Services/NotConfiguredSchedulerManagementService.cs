using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

internal sealed class NotConfiguredSchedulerManagementService : ISchedulerManagementService
{
    public Task<bool> TriggerJobAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> PauseJobAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> ResumeJobAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> DeleteJobAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> PauseTriggerAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> ResumeTriggerAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> ResetTriggerFromErrorAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task<bool> UnscheduleTriggerAsync(string group, string name, CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task PauseAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ResumeAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<RescheduleResult> RescheduleAsync(
        string group,
        string name,
        RescheduleRequestModel request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new RescheduleResult(RescheduleStatus.TriggerNotFound, "The NJobDesk scheduler is not configured."));
}

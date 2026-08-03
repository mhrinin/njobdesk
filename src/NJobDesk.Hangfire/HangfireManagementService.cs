using Hangfire;
using Hangfire.Storage;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace NJobDesk.Hangfire;

internal sealed class HangfireManagementService(
    JobStorage jobStorage,
    IRecurringJobManager recurringJobManager) : ISchedulerManagementService
{
    private readonly UnsupportedSchedulerManagementService _unsupported = new();

    public Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (!Exists(jobId))
        {
            return Task.FromResult(false);
        }

        recurringJobManager.Trigger(jobId);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        RemoveAsync(jobId);

    public Task<bool> UnscheduleTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        RemoveAsync(triggerId);

    public Task<bool> PauseJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        _unsupported.PauseJobAsync(jobId, cancellationToken);

    public Task<bool> ResumeJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        _unsupported.ResumeJobAsync(jobId, cancellationToken);

    public Task<bool> PauseTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        _unsupported.PauseTriggerAsync(triggerId, cancellationToken);

    public Task<bool> ResumeTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        _unsupported.ResumeTriggerAsync(triggerId, cancellationToken);

    public Task<bool> ResetTriggerFromErrorAsync(string triggerId, CancellationToken cancellationToken = default) =>
        _unsupported.ResetTriggerFromErrorAsync(triggerId, cancellationToken);

    public Task PauseAllAsync(CancellationToken cancellationToken = default) =>
        _unsupported.PauseAllAsync(cancellationToken);

    public Task ResumeAllAsync(CancellationToken cancellationToken = default) =>
        _unsupported.ResumeAllAsync(cancellationToken);

    public Task<RescheduleResult> RescheduleAsync(
        string triggerId,
        RescheduleRequestModel request,
        CancellationToken cancellationToken = default) =>
        _unsupported.RescheduleAsync(triggerId, request, cancellationToken);

    private Task<bool> RemoveAsync(string jobId)
    {
        if (!Exists(jobId))
        {
            return Task.FromResult(false);
        }

        recurringJobManager.RemoveIfExists(jobId);
        return Task.FromResult(true);
    }

    // GetRecurringJobs(ids) returns a placeholder marked Removed for ids that don't exist.
    private bool Exists(string jobId)
    {
        using var connection = jobStorage.GetConnection();
        return connection.GetRecurringJobs([jobId]).Any(dto => !dto.Removed);
    }
}

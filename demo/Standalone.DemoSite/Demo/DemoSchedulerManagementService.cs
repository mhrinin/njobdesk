using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace Standalone.DemoSite.Demo;

internal sealed class DemoSchedulerManagementService(
    DemoSchedulerState state,
    ICronService cronService,
    ISchedulerInfoService infoService) : ISchedulerManagementService
{
    public Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (state.FindJob(jobId) is not { } job)
        {
            return Task.FromResult(false);
        }

        var executionId = state.StartRun(job);
        _ = CompleteLaterAsync(executionId, failed: job.Trigger?.State == JobState.Error);
        return Task.FromResult(true);
    }

    public Task<bool> PauseJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetJobState(jobId, JobState.Paused));

    public Task<bool> ResumeJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetJobState(jobId, JobState.Normal));

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.RemoveJob(jobId));

    public Task<bool> PauseTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetTriggerState(triggerId, JobState.None, JobState.Paused));

    public Task<bool> ResumeTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetTriggerState(triggerId, JobState.None, JobState.Normal));

    public Task<bool> ResetTriggerFromErrorAsync(string triggerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetTriggerState(triggerId, JobState.Error, JobState.Normal));

    public Task<bool> UnscheduleTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.RemoveTrigger(triggerId));

    public Task PauseAllAsync(CancellationToken cancellationToken = default)
    {
        state.SetAllTriggers(JobState.Paused);
        return Task.CompletedTask;
    }

    public Task ResumeAllAsync(CancellationToken cancellationToken = default)
    {
        state.SetAllTriggers(JobState.Normal);
        return Task.CompletedTask;
    }

    public async Task<RescheduleResult> RescheduleAsync(
        string triggerId, RescheduleRequestModel request, CancellationToken cancellationToken = default)
    {
        var trigger = state.Jobs.Select(job => job.Trigger)
            .FirstOrDefault(candidate => candidate is not null && candidate.Id == triggerId);
        if (trigger is null)
        {
            return new RescheduleResult(RescheduleStatus.TriggerNotFound);
        }

        var validation = cronService.Validate(request.CronExpression, nextFireTimeCount: 1, request.TimeZoneId);
        if (!validation.IsValid)
        {
            return new RescheduleResult(RescheduleStatus.InvalidCronExpression, validation.Error);
        }

        trigger.CronExpression = request.CronExpression;
        trigger.TimeZoneId = request.TimeZoneId;
        var updated = await infoService.GetTriggerAsync(triggerId, cancellationToken);
        return new RescheduleResult(RescheduleStatus.Success, Trigger: updated);
    }

    private async Task CompleteLaterAsync(long executionId, bool failed)
    {
        await Task.Delay(TimeSpan.FromSeconds(4));
        state.CompleteRun(executionId, failed);
    }
}

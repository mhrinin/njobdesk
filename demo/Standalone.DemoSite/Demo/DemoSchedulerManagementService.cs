using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace Standalone.DemoSite.Demo;

internal sealed class DemoSchedulerManagementService(
    DemoSchedulerState state,
    ICronService cronService,
    ISchedulerInfoService infoService) : ISchedulerManagementService
{
    public Task<bool> TriggerJobAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        if (state.FindJob(group, name) is not { } job)
        {
            return Task.FromResult(false);
        }

        var executionId = state.StartRun(job);
        _ = CompleteLaterAsync(executionId, failed: job.Trigger?.State == JobState.Error);
        return Task.FromResult(true);
    }

    public Task<bool> PauseJobAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetJobState(group, name, JobState.Paused));

    public Task<bool> ResumeJobAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetJobState(group, name, JobState.Normal));

    public Task<bool> DeleteJobAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.RemoveJob(group, name));

    public Task<bool> PauseTriggerAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetTriggerState(group, name, JobState.None, JobState.Paused));

    public Task<bool> ResumeTriggerAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetTriggerState(group, name, JobState.None, JobState.Normal));

    public Task<bool> ResetTriggerFromErrorAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.SetTriggerState(group, name, JobState.Error, JobState.Normal));

    public Task<bool> UnscheduleTriggerAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.RemoveTrigger(group, name));

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
        string group, string name, RescheduleRequestModel request, CancellationToken cancellationToken = default)
    {
        var trigger = state.Jobs.Select(job => job.Trigger)
            .FirstOrDefault(t => t is not null && t.Group == group && t.Name == name);
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
        var updated = await infoService.GetTriggerAsync(group, name, cancellationToken);
        return new RescheduleResult(RescheduleStatus.Success, Trigger: updated);
    }

    private async Task CompleteLaterAsync(long executionId, bool failed)
    {
        await Task.Delay(TimeSpan.FromSeconds(4));
        state.CompleteRun(executionId, failed);
    }
}

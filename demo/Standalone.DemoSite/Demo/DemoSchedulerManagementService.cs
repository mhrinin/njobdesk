using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;
using NJobDesk.History.EFCore.Capture;

namespace Standalone.DemoSite.Demo;

internal sealed class DemoSchedulerManagementService(
    DemoSchedulerState state,
    ICronService cronService,
    ISchedulerInfoService infoService,
    IExecutionHistoryWriter historyWriter,
    IExecutionLogCapture logCapture,
    IExecutionLogStore logStore,
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider) : ISchedulerManagementService
{
    public async Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (state.FindJob(jobId) is not { } job)
        {
            return false;
        }

        var fireInstanceId = Guid.NewGuid().ToString("N");
        await historyWriter.StartAsync(
            new JobExecutionHistory
            {
                FireInstanceId = fireInstanceId,
                SchedulerInstanceId = "demo-instance",
                ProviderKey = DemoSchedulerProvider.Key,
                JobId = job.Id,
                JobName = job.Name,
                JobGroup = job.Group,
                TriggerId = job.Trigger?.Id,
                TriggerName = job.Trigger?.Name ?? $"{job.Name}-trigger",
                StartedUtc = timeProvider.GetUtcNow().UtcDateTime,
                Status = ExecutionStatus.Running,
            },
            cancellationToken);
        _ = RunLaterAsync(job, fireInstanceId, failed: job.Trigger?.State == JobState.Error);
        return true;
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

    // Simulates the run: everything logged on this async flow while the capture scope is active is
    // persisted against the run, exactly as a real provider integration would do it.
    private async Task RunLaterAsync(DemoJob job, string fireInstanceId, bool failed)
    {
        var logger = loggerFactory.CreateLogger($"Demo.Jobs.{job.JobType}");
        var scope = logCapture.BeginScope();
        try
        {
            logger.LogInformation("Job started (manual trigger).");
            await Task.Delay(TimeSpan.FromSeconds(4));

            if (failed)
            {
                scope?.RecordException(
                    $"Demo.Jobs.{job.JobType}",
                    new HttpRequestException("Response status code does not indicate success: 503 (Service Unavailable)."));
                logger.LogInformation("Job finished with errors.");
            }
            else
            {
                logger.LogInformation("Job finished.");
            }
        }
        finally
        {
            scope?.Dispose();
        }

        await historyWriter.CompleteAsync(
            DemoSchedulerProvider.Key,
            fireInstanceId,
            failed ? ExecutionStatus.Failed : ExecutionStatus.Succeeded,
            failed ? "Demo failure: upstream service returned 503." : null);
        if (scope is not null)
        {
            await logStore.SaveAsync(DemoSchedulerProvider.Key, fireInstanceId, scope);
        }
    }
}

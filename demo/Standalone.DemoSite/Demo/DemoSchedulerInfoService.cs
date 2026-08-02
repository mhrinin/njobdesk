using NJobDesk.Core.Contracts;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;

namespace Standalone.DemoSite.Demo;

internal sealed class DemoSchedulerInfoService(
    DemoSchedulerState state,
    IExecutionHistoryStore historyStore,
    TimeProvider timeProvider) : ISchedulerInfoService
{
    public Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchedulerStatusModel
        {
            State = state.Paused ? SchedulerState.Standby : SchedulerState.Started,
            SchedulerName = "DemoScheduler",
            SchedulerInstanceId = "demo-instance",
            Clustered = false,
            SchedulerEnabled = true,
            HistoryEnabled = true,
            StoreType = "InMemoryDemoStore",
            ThreadPoolSize = 4,
            RunningSinceUtc = state.StartedUtc,
            ProviderVersion = "demo",
        });

    public async Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = await historyStore.GetStatisticsAsync(cancellationToken);
        var jobs = state.Jobs;
        return new SchedulerStatisticsModel
        {
            JobsTotal = jobs.Count,
            JobsPaused = jobs.Count(job => job.Trigger?.State == JobState.Paused),
            RunningCount = statistics.RunningCount,
            Succeeded24h = statistics.Succeeded24h,
            Failed24h = statistics.Failed24h,
            Buckets = [.. statistics.Buckets.Select(bucket => new ExecutionBucketModel
            {
                HourStartUtc = bucket.HourStartUtc,
                Succeeded = bucket.Succeeded,
                Failed = bucket.Failed,
            })],
        };
    }

    public Task<PagedResult<JobSummaryModel>> GetJobsAsync(
        int skip, int take, string? group = null, string? filter = null, CancellationToken cancellationToken = default)
    {
        var jobs = state.Jobs
            .Where(job => group is null || job.Group == group)
            .Where(job => filter is null
                || job.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || job.Group.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(job => job.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var page = jobs.Skip(skip).Take(take).Select(MapSummary);
        return Task.FromResult(new PagedResult<JobSummaryModel>(jobs.Count, page.ToList()));
    }

    public async Task<JobDetailModel?> GetJobAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        if (state.FindJob(group, name) is not { } job)
        {
            return null;
        }

        var recent = await historyStore.GetPageAsync(
            new ExecutionHistoryFilter { JobGroup = group, JobName = name, Take = 10 }, cancellationToken);

        return new JobDetailModel
        {
            Job = MapSummary(job),
            Triggers = job.Trigger is { } trigger ? [MapTrigger(trigger)] : [],
            RecentExecutions = [.. recent.Items.Select(ExecutionModelMapper.MapExecution)],
        };
    }

    public Task<TriggerModel?> GetTriggerAsync(string group, string name, CancellationToken cancellationToken = default)
    {
        var trigger = state.Jobs.Select(job => job.Trigger)
            .FirstOrDefault(t => t is not null && t.Group == group && t.Name == name);
        return Task.FromResult(trigger is null ? null : MapTrigger(trigger));
    }

    public async Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        var running = await historyStore.GetRunningAsync(cancellationToken);
        return [.. running.Select(ExecutionModelMapper.MapExecution)];
    }

    private JobSummaryModel MapSummary(DemoJob job)
    {
        var trigger = job.Trigger;
        return new JobSummaryModel
        {
            Group = job.Group,
            Name = job.Name,
            Description = job.Description,
            JobType = $"Demo.Jobs.{job.JobType}",
            Durable = trigger is null,
            ConcurrentExecutionDisallowed = true,
            TriggerCount = trigger is null ? 0 : 1,
            ScheduleSummary = trigger is null ? null : CronDescriptions.Describe(trigger.CronExpression),
            State = trigger?.State ?? JobState.None,
            NextFireTimeUtc = trigger?.State is JobState.Normal ? trigger.NextFireTimeUtc(timeProvider) : null,
            PreviousFireTimeUtc = null,
            IsSystemJob = job.IsSystemJob,
        };
    }

    private TriggerModel MapTrigger(DemoTrigger trigger) => new()
    {
        Group = trigger.Group,
        Name = trigger.Name,
        Type = TriggerType.Cron,
        CronExpression = trigger.CronExpression,
        CronSummary = CronDescriptions.Describe(trigger.CronExpression),
        TimeZoneId = trigger.TimeZoneId ?? TimeZoneInfo.Utc.Id,
        State = trigger.State,
        NextFireTimeUtc = trigger.State is JobState.Normal ? trigger.NextFireTimeUtc(timeProvider) : null,
        PreviousFireTimeUtc = null,
        StartTimeUtc = trigger.StartTimeUtc,
        MisfireInstruction = "SmartPolicy",
        Priority = 5,
    };
}

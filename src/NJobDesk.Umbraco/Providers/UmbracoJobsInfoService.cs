using NJobDesk.Core.Contracts;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace NJobDesk.Umbraco.Providers;

internal sealed class UmbracoJobsInfoService(
    IEnumerable<IRecurringBackgroundJob> jobs,
    IExecutionHistoryStore historyStore,
    TimeProvider timeProvider) : ISchedulerInfoService
{
    private readonly DateTime _startedUtc = timeProvider.GetUtcNow().UtcDateTime;

    public Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchedulerStatusModel
        {
            State = SchedulerState.Started,
            SchedulerName = "Umbraco recurring jobs",
            SchedulerInstanceId = Environment.MachineName,
            HistoryEnabled = true,
            RunningSinceUtc = _startedUtc,
        });

    public async Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = await historyStore.GetStatisticsAsync(cancellationToken);
        return new SchedulerStatisticsModel
        {
            JobsTotal = jobs.Count(),
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

    public async Task<PagedResult<JobSummaryModel>> GetJobsAsync(
        int skip, int take, string? group = null, string? filter = null, CancellationToken cancellationToken = default)
    {
        var matching = jobs
            .Where(job => filter is null
                || UmbracoRecurringJobsProvider.JobId(job).Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(job => job.GetType().Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        List<JobSummaryModel> page = [];
        foreach (var job in matching.Skip(skip).Take(take))
        {
            page.Add(await MapSummaryAsync(job, cancellationToken));
        }

        return new PagedResult<JobSummaryModel>(matching.Count, page);
    }

    public async Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (Find(jobId) is not { } job)
        {
            return null;
        }

        var summary = await MapSummaryAsync(job, cancellationToken);
        var recent = await historyStore.GetPageAsync(
            new ExecutionHistoryFilter { ProviderKey = UmbracoRecurringJobsProvider.Key, JobId = jobId, Take = 10 },
            cancellationToken);

        return new JobDetailModel
        {
            Job = summary,
            Triggers = [SyntheticTrigger(job, summary)],
            RecentExecutions = [.. recent.Items.Select(ExecutionModelMapper.MapExecution)],
        };
    }

    public async Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default)
    {
        if (Find(triggerId) is not { } job)
        {
            return null;
        }

        return SyntheticTrigger(job, await MapSummaryAsync(job, cancellationToken));
    }

    public async Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        var running = await historyStore.GetRunningAsync(cancellationToken);
        return [.. running
            .Where(entry => entry.ProviderKey == UmbracoRecurringJobsProvider.Key)
            .Select(ExecutionModelMapper.MapExecution)];
    }

    private IRecurringBackgroundJob? Find(string jobId) =>
        jobs.FirstOrDefault(job => UmbracoRecurringJobsProvider.JobId(job) == jobId);

    private async Task<JobSummaryModel> MapSummaryAsync(IRecurringBackgroundJob job, CancellationToken cancellationToken)
    {
        var jobId = UmbracoRecurringJobsProvider.JobId(job);
        var lastRun = (await historyStore.GetPageAsync(
                new ExecutionHistoryFilter { ProviderKey = UmbracoRecurringJobsProvider.Key, JobId = jobId, Take = 1 },
                cancellationToken))
            .Items.FirstOrDefault();

        return new JobSummaryModel
        {
            Id = jobId,
            Name = job.GetType().Name,
            JobType = jobId,
            TriggerCount = 1,
            ScheduleSummary = DescribePeriod(job.Period),
            State = JobState.Normal,
            NextFireTimeUtc = lastRun is null ? null : lastRun.StartedUtc + job.Period,
            PreviousFireTimeUtc = lastRun?.StartedUtc,
            IsSystemJob = jobId.StartsWith("Umbraco.", StringComparison.Ordinal),
            Capabilities = UmbracoRecurringJobsProvider.DefaultCapabilities with
            {
                TriggerNow = UmbracoRecurringJobsProvider.CanTrigger(job),
            },
        };
    }

    private TriggerModel SyntheticTrigger(IRecurringBackgroundJob job, JobSummaryModel summary) => new()
    {
        Id = summary.Id,
        Name = $"{summary.Name}-schedule",
        Type = TriggerType.Simple,
        CronSummary = summary.ScheduleSummary,
        State = JobState.Normal,
        NextFireTimeUtc = summary.NextFireTimeUtc,
        PreviousFireTimeUtc = summary.PreviousFireTimeUtc,
        StartTimeUtc = _startedUtc + job.Delay,
    };

    internal static string DescribePeriod(TimeSpan period) => period switch
    {
        { TotalSeconds: < 120 } => $"Every {period.TotalSeconds:0} seconds",
        { TotalMinutes: < 120 } => $"Every {period.TotalMinutes:0} minutes",
        { TotalHours: < 48 } => $"Every {period.TotalHours:0.#} hours",
        _ => $"Every {period.TotalDays:0.#} days",
    };
}

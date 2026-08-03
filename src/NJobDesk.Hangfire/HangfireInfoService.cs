using Hangfire;
using Hangfire.Storage;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace NJobDesk.Hangfire;

internal sealed class HangfireInfoService(JobStorage jobStorage, TimeProvider timeProvider) : ISchedulerInfoService
{
    public Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var servers = jobStorage.GetMonitoringApi().Servers();
        return Task.FromResult(new SchedulerStatusModel
        {
            State = servers.Count > 0 ? SchedulerState.Started : SchedulerState.Standby,
            SchedulerName = "Hangfire",
            SchedulerInstanceId = servers.FirstOrDefault()?.Name,
            Clustered = servers.Count > 1,
            HistoryEnabled = false,
            StoreType = jobStorage.GetType().Name,
            ThreadPoolSize = servers.Count > 0 ? servers.Sum(server => server.WorkersCount) : null,
            RunningSinceUtc = servers.Count > 0 ? servers.Min(server => server.StartedAt) : null,
        });
    }

    public Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var statistics = jobStorage.GetMonitoringApi().GetStatistics();
        return Task.FromResult(new SchedulerStatisticsModel
        {
            JobsTotal = (int)statistics.Recurring,
            RunningCount = (int)statistics.Processing,
        });
    }

    public Task<PagedResult<JobSummaryModel>> GetJobsAsync(
        int skip, int take, string? group = null, string? filter = null, CancellationToken cancellationToken = default)
    {
        var jobs = GetRecurringJobs()
            .Where(dto => filter is null || dto.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(dto => dto.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult(new PagedResult<JobSummaryModel>(
            jobs.Count,
            jobs.Skip(skip).Take(take).Select(MapSummary).ToList()));
    }

    public Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (Find(jobId) is not { } dto)
        {
            return Task.FromResult<JobDetailModel?>(null);
        }

        return Task.FromResult<JobDetailModel?>(new JobDetailModel
        {
            Job = MapSummary(dto),
            Triggers = [MapTrigger(dto)],
            RecentExecutions = LastExecutionOf(dto) is { } execution ? [execution] : [],
        });
    }

    public Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Find(triggerId) is { } dto ? MapTrigger(dto) : null);

    public Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExecutionModel>>([]);

    private RecurringJobDto? Find(string jobId) =>
        GetRecurringJobs().FirstOrDefault(dto => dto.Id == jobId);

    private List<RecurringJobDto> GetRecurringJobs()
    {
        using var connection = jobStorage.GetConnection();
        return connection.GetRecurringJobs();
    }

    private JobSummaryModel MapSummary(RecurringJobDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Id,
        JobType = dto.Job?.ToString(),
        TriggerCount = 1,
        ScheduleSummary = Describe(dto.Cron),
        State = JobState.Normal,
        NextFireTimeUtc = dto.NextExecution,
        PreviousFireTimeUtc = dto.LastExecution,
        Capabilities = HangfireSchedulerProvider.DefaultCapabilities,
    };

    private TriggerModel MapTrigger(RecurringJobDto dto) => new()
    {
        Id = dto.Id,
        Name = $"{dto.Id}-trigger",
        Type = TriggerType.Cron,
        CronExpression = dto.Cron,
        CronSummary = Describe(dto.Cron),
        TimeZoneId = dto.TimeZoneId,
        State = JobState.Normal,
        NextFireTimeUtc = dto.NextExecution,
        PreviousFireTimeUtc = dto.LastExecution,
        StartTimeUtc = dto.CreatedAt ?? timeProvider.GetUtcNow().UtcDateTime,
    };

    // Last-execution only: Hangfire keeps the last run's state on the recurring job itself; deeper
    // history would require correlating the monitoring API's succeeded/failed pages per job.
    private static ExecutionModel? LastExecutionOf(RecurringJobDto dto)
    {
        if (dto.LastExecution is not { } lastExecution)
        {
            return null;
        }

        var state = dto.LastJobState switch
        {
            "Succeeded" => ExecutionStatus.Succeeded,
            "Failed" => ExecutionStatus.Failed,
            "Processing" or "Enqueued" or "Scheduled" => ExecutionStatus.Running,
            _ => (ExecutionStatus?)null,
        };
        if (state is null)
        {
            return null;
        }

        return new ExecutionModel
        {
            JobId = dto.Id,
            JobName = dto.Id,
            TriggerName = $"{dto.Id}-trigger",
            StartedUtc = lastExecution,
            FinishedUtc = state is ExecutionStatus.Running ? null : lastExecution,
            State = state.Value,
            SchedulerInstanceId = dto.LastJobId ?? "hangfire",
        };
    }

    private static string? Describe(string? cron) => cron is null ? null : CronDescriptions.Describe(cron);
}

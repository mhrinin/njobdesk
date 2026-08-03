using NJobDesk.Core.Contracts;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;

namespace Demo.Fakes;

/// <summary>
/// A small in-memory provider for the Umbraco demo hosts: a few cron jobs whose trigger/pause state
/// mutates so the dashboard's actions are observable without a real scheduler or history storage.
/// </summary>
public sealed class FakeSchedulerProvider : ISchedulerProvider
{
    private readonly object _lock = new();
    private readonly TimeProvider _timeProvider;
    private readonly DateTime _startedUtc;
    private readonly List<FakeJob> _jobs;

    public FakeSchedulerProvider(ICronService cronService, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _startedUtc = timeProvider.GetUtcNow().UtcDateTime;
        _jobs =
        [
            new FakeJob("content-refresh", "Republishes content whose release date passed.", "*/5 * * * *"),
            new FakeJob("search-reindex", "Rebuilds the site search index.", "0 2 * * *"),
            new FakeJob("form-export", "Exports form submissions to the CRM.", "15 * * * *"),
        ];
        Info = new FakeInfoService(this, cronService);
        Management = new FakeManagementService(this);
    }

    public SchedulerProviderDescriptor Descriptor { get; } = new()
    {
        Key = "fake",
        DisplayName = "Fake Scheduler",
        ProviderVersion = "0.1.0-demo",
        Capabilities = new SchedulerCapabilities
        {
            TriggerNow = true,
            Pause = true,
            Triggers = true,
        },
    };

    public ISchedulerInfoService Info { get; }

    public ISchedulerManagementService Management { get; }

    private FakeJob? Find(string jobId)
    {
        lock (_lock)
        {
            return _jobs.FirstOrDefault(job => job.Id == jobId);
        }
    }

    private sealed class FakeJob(string id, string description, string cron)
    {
        public string Id { get; } = id;

        public string Description { get; } = description;

        public string Cron { get; } = cron;

        public JobState State { get; set; } = JobState.Normal;
    }

    private sealed class FakeInfoService(FakeSchedulerProvider provider, ICronService cronService) : ISchedulerInfoService
    {
        public Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchedulerStatusModel
            {
                State = SchedulerState.Started,
                SchedulerName = "FakeScheduler",
                SchedulerInstanceId = Environment.MachineName,
                RunningSinceUtc = provider._startedUtc,
            });

        public Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default)
        {
            lock (provider._lock)
            {
                return Task.FromResult(new SchedulerStatisticsModel
                {
                    JobsTotal = provider._jobs.Count,
                    JobsPaused = provider._jobs.Count(job => job.State == JobState.Paused),
                });
            }
        }

        public Task<PagedResult<JobSummaryModel>> GetJobsAsync(
            int skip, int take, string? group = null, string? filter = null, CancellationToken cancellationToken = default)
        {
            lock (provider._lock)
            {
                var jobs = provider._jobs
                    .Where(job => filter is null || job.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
                    .Select(MapSummary)
                    .ToList();
                return Task.FromResult(new PagedResult<JobSummaryModel>(jobs.Count, jobs.Skip(skip).Take(take).ToList()));
            }
        }

        public Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var job = provider.Find(jobId);
            return Task.FromResult<JobDetailModel?>(job is null
                ? null
                : new JobDetailModel { Job = MapSummary(job), Triggers = [MapTrigger(job)] });
        }

        public Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default)
        {
            var job = provider.Find(triggerId);
            return Task.FromResult(job is null ? null : MapTrigger(job));
        }

        public Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExecutionModel>>([]);

        private JobSummaryModel MapSummary(FakeJob job) => new()
        {
            Id = job.Id,
            Name = job.Id,
            Description = job.Description,
            JobType = $"Demo.Fakes.Jobs.{job.Id}",
            TriggerCount = 1,
            ScheduleSummary = CronDescriptions.Describe(job.Cron),
            State = job.State,
            NextFireTimeUtc = NextFireUtc(job),
            Capabilities = provider.Descriptor.Capabilities,
        };

        private TriggerModel MapTrigger(FakeJob job) => new()
        {
            Id = job.Id,
            Name = $"{job.Id}-trigger",
            Type = TriggerType.Cron,
            CronExpression = job.Cron,
            CronSummary = CronDescriptions.Describe(job.Cron),
            TimeZoneId = TimeZoneInfo.Utc.Id,
            State = job.State,
            NextFireTimeUtc = NextFireUtc(job),
            StartTimeUtc = provider._startedUtc,
        };

        private DateTime? NextFireUtc(FakeJob job) =>
            job.State is JobState.Normal
                && cronService.Validate(job.Cron, nextFireTimeCount: 1, TimeZoneInfo.Utc.Id) is { IsValid: true } validation
                    ? validation.NextFireTimesUtc.FirstOrDefault()
                    : null;
    }

    private sealed class FakeManagementService(FakeSchedulerProvider provider) : ISchedulerManagementService
    {
        public Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(provider.Find(jobId) is not null);

        public Task<bool> PauseJobAsync(string jobId, CancellationToken cancellationToken = default) =>
            SetState(jobId, JobState.Paused);

        public Task<bool> ResumeJobAsync(string jobId, CancellationToken cancellationToken = default) =>
            SetState(jobId, JobState.Normal);

        public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<bool> PauseTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
            SetState(triggerId, JobState.Paused);

        public Task<bool> ResumeTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
            SetState(triggerId, JobState.Normal);

        public Task<bool> ResetTriggerFromErrorAsync(string triggerId, CancellationToken cancellationToken = default) =>
            SetState(triggerId, JobState.Normal);

        public Task<bool> UnscheduleTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task PauseAllAsync(CancellationToken cancellationToken = default) => SetAll(JobState.Paused);

        public Task ResumeAllAsync(CancellationToken cancellationToken = default) => SetAll(JobState.Normal);

        public Task<RescheduleResult> RescheduleAsync(
            string triggerId, RescheduleRequestModel request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new RescheduleResult(RescheduleStatus.NotSupported, "The fake scheduler cannot reschedule."));

        private Task<bool> SetState(string jobId, JobState state)
        {
            if (provider.Find(jobId) is not { } job)
            {
                return Task.FromResult(false);
            }

            job.State = state;
            return Task.FromResult(true);
        }

        private Task SetAll(JobState state)
        {
            lock (provider._lock)
            {
                foreach (var job in provider._jobs)
                {
                    job.State = state;
                }
            }

            return Task.CompletedTask;
        }
    }
}

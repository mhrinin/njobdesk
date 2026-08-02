using NJobDesk.Core.Contracts;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;

namespace Standalone.DemoSite.Demo;

/// <summary>
/// A deliberately limited provider: read-only jobs without groups, trigger entities, or history —
/// the shape of simple schedulers such as Umbraco's recurring jobs. Exercises capability gating and
/// synthetic-trigger projection in the dashboard.
/// </summary>
internal sealed class BasicSchedulerProvider(ICronService cronService, TimeProvider timeProvider) : ISchedulerProvider
{
    private const string BackupJobId = "nightly-backup";
    private const string BackupCron = "30 2 * * *";
    private const string TelemetryJobId = "telemetry-flush";
    private static readonly TimeSpan TelemetryInterval = TimeSpan.FromMinutes(30);

    public SchedulerProviderDescriptor Descriptor { get; } = new()
    {
        Key = "basic",
        DisplayName = "Basic Scheduler",
        ProviderVersion = "0.9.0-demo",
        Capabilities = SchedulerCapabilities.None,
    };

    public ISchedulerInfoService Info => new BasicInfoService(this);

    public ISchedulerManagementService Management { get; } = new UnsupportedSchedulerManagementService();

    private JobSummaryModel[] Jobs =>
    [
        new()
        {
            Id = BackupJobId,
            Name = BackupJobId,
            Description = "Writes the nightly database backup.",
            JobType = "Basic.Jobs.NightlyBackupJob",
            TriggerCount = 1,
            ScheduleSummary = CronDescriptions.Describe(BackupCron),
            State = JobState.Normal,
            NextFireTimeUtc = NextBackupFireUtc(),
            Capabilities = Descriptor.Capabilities,
        },
        new()
        {
            Id = TelemetryJobId,
            Name = TelemetryJobId,
            Description = "Flushes buffered telemetry to the collector.",
            JobType = "Basic.Jobs.TelemetryFlushJob",
            TriggerCount = 1,
            ScheduleSummary = "Every 30 minutes",
            State = JobState.Normal,
            NextFireTimeUtc = NextTelemetryFireUtc(),
            Capabilities = Descriptor.Capabilities,
        },
    ];

    private DateTime? NextBackupFireUtc() =>
        cronService.Validate(BackupCron, nextFireTimeCount: 1, TimeZoneInfo.Utc.Id) is { IsValid: true } validation
            ? validation.NextFireTimesUtc.FirstOrDefault()
            : null;

    private DateTime NextTelemetryFireUtc()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var elapsed = now.TimeOfDay.Ticks % TelemetryInterval.Ticks;
        return now.AddTicks(TelemetryInterval.Ticks - elapsed);
    }

    private TriggerModel SyntheticTrigger(JobSummaryModel job) => new()
    {
        Id = job.Id,
        Name = $"{job.Name}-schedule",
        Type = job.Id == BackupJobId ? TriggerType.Cron : TriggerType.Simple,
        CronExpression = job.Id == BackupJobId ? BackupCron : null,
        CronSummary = job.ScheduleSummary,
        TimeZoneId = TimeZoneInfo.Utc.Id,
        State = job.State,
        NextFireTimeUtc = job.NextFireTimeUtc,
        StartTimeUtc = timeProvider.GetUtcNow().UtcDateTime.Date,
    };

    private sealed class BasicInfoService(BasicSchedulerProvider provider) : ISchedulerInfoService
    {
        public Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchedulerStatusModel
            {
                State = SchedulerState.Started,
                SchedulerName = "BasicScheduler",
                HistoryEnabled = false,
            });

        public Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new SchedulerStatisticsModel { JobsTotal = provider.Jobs.Length });

        public Task<PagedResult<JobSummaryModel>> GetJobsAsync(
            int skip, int take, string? group = null, string? filter = null, CancellationToken cancellationToken = default)
        {
            var jobs = provider.Jobs
                .Where(job => filter is null || job.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToList();
            return Task.FromResult(new PagedResult<JobSummaryModel>(jobs.Count, jobs.Skip(skip).Take(take).ToList()));
        }

        public Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
        {
            var job = provider.Jobs.FirstOrDefault(candidate => candidate.Id == jobId);
            return Task.FromResult<JobDetailModel?>(job is null
                ? null
                : new JobDetailModel { Job = job, Triggers = [provider.SyntheticTrigger(job)] });
        }

        public Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default)
        {
            var job = provider.Jobs.FirstOrDefault(candidate => candidate.Id == triggerId);
            return Task.FromResult(job is null ? null : provider.SyntheticTrigger(job));
        }

        public Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ExecutionModel>>([]);
    }
}

using Hangfire;
using Hangfire.Common;
using Hangfire.InMemory;
using Microsoft.Extensions.Time.Testing;
using NJobDesk.Core.Entities;
using NJobDesk.Hangfire;

namespace NJobDesk.Tests.Hangfire;

public class HangfireSchedulerProviderTests
{
    private readonly InMemoryStorage _storage = new();
    private readonly RecurringJobManager _recurringJobs;
    private readonly HangfireSchedulerProvider _provider;

    public HangfireSchedulerProviderTests()
    {
        _recurringJobs = new RecurringJobManager(_storage);
        _provider = new HangfireSchedulerProvider(_storage, _recurringJobs, new FakeTimeProvider());
    }

    [Fact]
    public async Task Lists_recurring_jobs_with_cron_and_next_run()
    {
        _recurringJobs.AddOrUpdate("night-report", Job.FromExpression(() => SampleJobs.Run()), "0 3 * * *");

        var page = await _provider.Info.GetJobsAsync(0, 10);

        var job = Assert.Single(page.Items);
        Assert.Equal("night-report", job.Id);
        Assert.Equal("At 03:00", job.ScheduleSummary);
        Assert.NotNull(job.NextFireTimeUtc);
        Assert.True(job.Capabilities.TriggerNow);
        Assert.False(job.Capabilities.ScheduleEditing);
    }

    [Fact]
    public async Task Job_detail_carries_the_cron_trigger_and_last_execution()
    {
        _recurringJobs.AddOrUpdate("night-report", Job.FromExpression(() => SampleJobs.Run()), "0 3 * * *");
        _recurringJobs.Trigger("night-report");

        var detail = await _provider.Info.GetJobAsync("night-report");

        Assert.NotNull(detail);
        var trigger = Assert.Single(detail.Triggers);
        Assert.Equal("0 3 * * *", trigger.CronExpression);
        var execution = Assert.Single(detail.RecentExecutions);
        Assert.Equal(ExecutionStatus.Running, execution.State);
    }

    [Fact]
    public async Task Trigger_enqueues_the_job_and_unknown_ids_are_refused()
    {
        _recurringJobs.AddOrUpdate("night-report", Job.FromExpression(() => SampleJobs.Run()), "0 3 * * *");

        Assert.True(await _provider.Management.TriggerJobAsync("night-report"));
        Assert.False(await _provider.Management.TriggerJobAsync("missing"));
        Assert.Equal(1, _storage.GetMonitoringApi().EnqueuedCount("default"));
    }

    [Fact]
    public async Task Delete_removes_the_recurring_job()
    {
        _recurringJobs.AddOrUpdate("night-report", Job.FromExpression(() => SampleJobs.Run()), "0 3 * * *");

        Assert.True(await _provider.Management.DeleteJobAsync("night-report"));
        Assert.False(await _provider.Management.DeleteJobAsync("night-report"));
        Assert.Empty((await _provider.Info.GetJobsAsync(0, 10)).Items);
    }

    [Fact]
    public async Task Statistics_report_the_recurring_job_count()
    {
        _recurringJobs.AddOrUpdate("one", Job.FromExpression(() => SampleJobs.Run()), "0 3 * * *");
        _recurringJobs.AddOrUpdate("two", Job.FromExpression(() => SampleJobs.Run()), "0 4 * * *");

        var statistics = await _provider.Info.GetStatisticsAsync();

        Assert.Equal(2, statistics.JobsTotal);
    }

    public static class SampleJobs
    {
        public static void Run()
        {
        }
    }
}

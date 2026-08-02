using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NJobDesk.Tests.Providers;

public class AggregatingDashboardInfoServiceTests
{
    private readonly FakeTimeProvider _timeProvider = new();
    private readonly FakeSchedulerProvider _healthy = new("alpha");
    private readonly FakeSchedulerProvider _broken = new("broken");

    private AggregatingDashboardInfoService CreateService(params FakeSchedulerProvider[] providers) => new(
        new SchedulerProviderRegistry(providers),
        _timeProvider,
        NullLogger<AggregatingDashboardInfoService>.Instance);

    [Fact]
    public async Task Status_isolates_a_throwing_provider_as_degraded()
    {
        _healthy.Info.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new SchedulerStatusModel { State = SchedulerState.Started });
        _broken.Info.GetStatusAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("backend down"));

        var status = await CreateService(_healthy, _broken).GetStatusAsync();

        Assert.Equal(2, status.Providers.Count);
        var healthy = status.Providers[0];
        Assert.False(healthy.Degraded);
        Assert.Equal(SchedulerState.Started, healthy.Status!.State);
        Assert.True(healthy.Capabilities.TriggerNow);
        var degraded = status.Providers[1];
        Assert.True(degraded.Degraded);
        Assert.Equal("backend down", degraded.Error);
        Assert.Null(degraded.Status);
    }

    [Fact]
    public async Task Status_marks_a_hanging_provider_degraded_after_the_timeout()
    {
        _healthy.Info.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(new SchedulerStatusModel { State = SchedulerState.Started });
        _broken.Info.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, callInfo.Arg<CancellationToken>());
                return new SchedulerStatusModel { State = SchedulerState.Started };
            });

        var pending = CreateService(_healthy, _broken).GetStatusAsync();
        _timeProvider.Advance(AggregatingDashboardInfoService.ProviderCallTimeout + TimeSpan.FromSeconds(1));
        var status = await pending;

        Assert.False(status.Providers[0].Degraded);
        Assert.True(status.Providers[1].Degraded);
    }

    [Fact]
    public async Task Jobs_are_merged_stamped_and_totalled_despite_a_broken_provider()
    {
        _healthy.Info.GetJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<JobSummaryModel>(2, [Job("cleanup"), Job("sync")]));
        _broken.Info.GetJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("backend down"));

        var page = await CreateService(_healthy, _broken).GetJobsAsync(0, 10);

        Assert.Equal(2, page.Total);
        Assert.Equal(["alpha:cleanup", "alpha:sync"], page.Items.Select(job => job.Id));
        Assert.All(page.Items, job => Assert.Equal("alpha", job.ProviderKey));
    }

    [Fact]
    public async Task Jobs_can_be_filtered_to_a_single_provider()
    {
        var second = new FakeSchedulerProvider("beta");
        second.Info.GetJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<JobSummaryModel>(1, [Job("beta-job")]));

        var page = await CreateService(_healthy, second).GetJobsAsync(0, 10, providerKey: "beta");

        Assert.Equal(["beta:beta-job"], page.Items.Select(job => job.Id));
        await _healthy.Info.DidNotReceive()
            .GetJobsAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Job_detail_routes_by_composite_id_and_stamps_the_result()
    {
        _healthy.Info.GetJobAsync("cleanup", Arg.Any<CancellationToken>())
            .Returns(new JobDetailModel
            {
                Job = Job("cleanup"),
                Triggers = [Trigger("cleanup-trigger")],
                RecentExecutions = [Execution("cleanup")],
            });

        var detail = await CreateService(_healthy).GetJobAsync("alpha:cleanup");

        Assert.NotNull(detail);
        Assert.Equal("alpha:cleanup", detail.Job.Id);
        Assert.Equal("alpha:cleanup-trigger", detail.Triggers[0].Id);
        Assert.Equal("alpha:cleanup", detail.RecentExecutions[0].JobId);
        Assert.Equal("alpha", detail.RecentExecutions[0].ProviderKey);
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("unknown:job")]
    public async Task Job_detail_is_null_for_unroutable_ids(string jobId) =>
        Assert.Null(await CreateService(_healthy).GetJobAsync(jobId));

    [Fact]
    public async Task Statistics_are_summed_and_buckets_merged_by_hour()
    {
        var hour = new DateTime(2026, 8, 2, 10, 0, 0, DateTimeKind.Utc);
        var second = new FakeSchedulerProvider("beta");
        _healthy.Info.GetStatisticsAsync(Arg.Any<CancellationToken>()).Returns(new SchedulerStatisticsModel
        {
            JobsTotal = 3,
            RunningCount = 1,
            Succeeded24h = 5,
            Buckets = [new ExecutionBucketModel { HourStartUtc = hour, Succeeded = 5 }],
        });
        second.Info.GetStatisticsAsync(Arg.Any<CancellationToken>()).Returns(new SchedulerStatisticsModel
        {
            JobsTotal = 2,
            JobsPaused = 1,
            Failed24h = 2,
            Buckets = [new ExecutionBucketModel { HourStartUtc = hour, Failed = 2 }],
        });

        var statistics = await CreateService(_healthy, second).GetStatisticsAsync();

        Assert.Equal(5, statistics.JobsTotal);
        Assert.Equal(1, statistics.JobsPaused);
        Assert.Equal(1, statistics.RunningCount);
        Assert.Equal(5, statistics.Succeeded24h);
        Assert.Equal(2, statistics.Failed24h);
        var bucket = Assert.Single(statistics.Buckets);
        Assert.Equal(5, bucket.Succeeded);
        Assert.Equal(2, bucket.Failed);
    }

    [Fact]
    public async Task Running_executions_are_stamped_with_their_provider()
    {
        _healthy.Info.GetRunningAsync(Arg.Any<CancellationToken>())
            .Returns([Execution("cleanup")]);

        var running = await CreateService(_healthy).GetRunningAsync();

        var execution = Assert.Single(running);
        Assert.Equal("alpha:cleanup", execution.JobId);
        Assert.Equal("alpha", execution.ProviderKey);
    }

    private static JobSummaryModel Job(string id) => new()
    {
        Id = id,
        Name = id,
        State = JobState.Normal,
        Capabilities = SchedulerCapabilities.Full,
    };

    private static TriggerModel Trigger(string id) => new()
    {
        Id = id,
        Name = id,
        Type = TriggerType.Cron,
        State = JobState.Normal,
    };

    private static ExecutionModel Execution(string jobId) => new()
    {
        JobId = jobId,
        JobName = jobId,
        State = Core.Entities.ExecutionStatus.Running,
        SchedulerInstanceId = "node-1",
    };
}

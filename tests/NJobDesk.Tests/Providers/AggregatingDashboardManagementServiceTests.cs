using Microsoft.Extensions.Logging.Abstractions;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace NJobDesk.Tests.Providers;

public class AggregatingDashboardManagementServiceTests
{
    private static AggregatingDashboardManagementService CreateService(params FakeSchedulerProvider[] providers) => new(
        new SchedulerProviderRegistry(providers),
        NullLogger<AggregatingDashboardManagementService>.Instance);

    [Fact]
    public async Task Routes_actions_to_the_owning_provider_with_the_local_id()
    {
        var provider = new FakeSchedulerProvider("alpha");
        provider.Management.TriggerJobAsync("cleanup", Arg.Any<CancellationToken>()).Returns(true);

        Assert.True(await CreateService(provider).TriggerJobAsync("alpha:cleanup"));
    }

    [Theory]
    [InlineData("malformed")]
    [InlineData("unknown:cleanup")]
    public async Task Refuses_unroutable_ids(string jobId)
    {
        var provider = new FakeSchedulerProvider("alpha");

        Assert.False(await CreateService(provider).TriggerJobAsync(jobId));
        await provider.Management.DidNotReceive().TriggerJobAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Refuses_actions_the_provider_capabilities_do_not_allow()
    {
        var provider = new FakeSchedulerProvider("alpha", SchedulerCapabilities.None);

        var service = CreateService(provider);

        Assert.False(await service.TriggerJobAsync("alpha:cleanup"));
        Assert.False(await service.PauseJobAsync("alpha:cleanup"));
        Assert.False(await service.DeleteJobAsync("alpha:cleanup"));
        Assert.False(await service.UnscheduleTriggerAsync("alpha:cleanup-trigger"));
        await provider.Management.DidNotReceive().TriggerJobAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await provider.Management.DidNotReceive().PauseJobAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Reschedule_reports_not_supported_without_the_capability()
    {
        var provider = new FakeSchedulerProvider(
            "alpha", SchedulerCapabilities.Full with { ScheduleEditing = false });

        var result = await CreateService(provider)
            .RescheduleAsync("alpha:cleanup-trigger", new RescheduleRequestModel { CronExpression = "0 0 * * *" });

        Assert.Equal(RescheduleStatus.NotSupported, result.Status);
    }

    [Fact]
    public async Task Reschedule_stamps_the_updated_trigger()
    {
        var provider = new FakeSchedulerProvider("alpha");
        provider.Management
            .RescheduleAsync("cleanup-trigger", Arg.Any<RescheduleRequestModel>(), Arg.Any<CancellationToken>())
            .Returns(RescheduleResult.Success(new TriggerModel
            {
                Id = "cleanup-trigger",
                Name = "cleanup-trigger",
                Type = TriggerType.Cron,
                State = JobState.Normal,
            }));

        var result = await CreateService(provider)
            .RescheduleAsync("alpha:cleanup-trigger", new RescheduleRequestModel { CronExpression = "0 0 * * *" });

        Assert.Equal(RescheduleStatus.Success, result.Status);
        Assert.Equal("alpha:cleanup-trigger", result.Trigger!.Id);
    }

    [Fact]
    public async Task Pause_all_targets_pausable_providers_and_isolates_failures()
    {
        var throwing = new FakeSchedulerProvider("throwing");
        throwing.Management.PauseAllAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("backend down"));
        var pausable = new FakeSchedulerProvider("pausable");
        var readOnly = new FakeSchedulerProvider("read-only", SchedulerCapabilities.None);

        await CreateService(throwing, pausable, readOnly).PauseAllAsync();

        await pausable.Management.Received(1).PauseAllAsync(Arg.Any<CancellationToken>());
        await readOnly.Management.DidNotReceive().PauseAllAsync(Arg.Any<CancellationToken>());
    }
}

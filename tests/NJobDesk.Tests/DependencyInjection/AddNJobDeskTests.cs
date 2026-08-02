using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NJobDesk.Core.DependencyInjection;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;
using NJobDesk.Tests.Providers;

namespace NJobDesk.Tests.DependencyInjection;

public class AddNJobDeskTests
{
    [Fact]
    public void Registers_scheduler_agnostic_defaults()
    {
        var provider = CreateServices().AddNJobDesk().Services.BuildServiceProvider();

        Assert.IsType<EmptyExecutionHistoryStore>(provider.GetRequiredService<IExecutionHistoryStore>());
        Assert.IsType<CronosCronService>(provider.GetRequiredService<ICronService>());
        Assert.Empty(provider.GetRequiredService<ISchedulerProviderRegistry>().Providers);
        Assert.IsType<AggregatingDashboardInfoService>(provider.GetRequiredService<IDashboardInfoService>());
        Assert.IsType<AggregatingDashboardManagementService>(provider.GetRequiredService<IDashboardManagementService>());
    }

    [Fact]
    public void Add_provider_plugs_schedulers_into_the_registry()
    {
        var services = CreateServices();
        services.AddSingleton<ISchedulerProvider>(new FakeSchedulerProvider("alpha"));

        var builder = services.AddNJobDesk();
        builder.AddProvider<BetaProvider>();

        var registry = builder.Services.BuildServiceProvider().GetRequiredService<ISchedulerProviderRegistry>();
        Assert.Equal(["alpha", "beta"], registry.Providers.Select(candidate => candidate.Descriptor.Key));
    }

    [Fact]
    public void Host_registrations_win_over_defaults()
    {
        var services = CreateServices();
        services.AddSingleton<ICronService, FakeCronService>();
        services.AddNJobDesk();

        var provider = services.BuildServiceProvider();

        Assert.IsType<FakeCronService>(provider.GetRequiredService<ICronService>());
    }

    [Fact]
    public void First_call_wins_the_section_name()
    {
        var services = CreateServices();
        var first = services.AddNJobDesk("Custom");
        var second = services.AddNJobDesk("Ignored");

        Assert.Equal("Custom", first.SectionName);
        Assert.Equal("Custom", second.SectionName);
    }

    private static ServiceCollection CreateServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        return services;
    }

    private sealed class BetaProvider : ISchedulerProvider
    {
        private readonly FakeSchedulerProvider _inner = new("beta");

        public SchedulerProviderDescriptor Descriptor => _inner.Descriptor;

        public ISchedulerInfoService Info => _inner.Info;

        public ISchedulerManagementService Management => _inner.Management;
    }

    private sealed class FakeCronService : ICronService
    {
        public Core.Models.CronValidationResultModel Validate(string cronExpression, int nextFireTimeCount = 5, string? timeZoneId = null) =>
            new() { IsValid = true };
    }
}

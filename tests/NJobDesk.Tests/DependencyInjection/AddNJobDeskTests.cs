using Microsoft.Extensions.DependencyInjection;
using NJobDesk.Core.DependencyInjection;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;

namespace NJobDesk.Tests.DependencyInjection;

public class AddNJobDeskTests
{
    [Fact]
    public void Registers_not_configured_defaults()
    {
        var provider = new ServiceCollection().AddNJobDesk().Services.BuildServiceProvider();

        Assert.IsType<NotConfiguredSchedulerInfoService>(provider.GetRequiredService<ISchedulerInfoService>());
        Assert.IsType<NotConfiguredSchedulerManagementService>(provider.GetRequiredService<ISchedulerManagementService>());
        Assert.IsType<EmptyExecutionHistoryStore>(provider.GetRequiredService<IExecutionHistoryStore>());
        Assert.IsType<CronosCronService>(provider.GetRequiredService<ICronService>());
    }

    [Fact]
    public void Provider_registrations_win_over_defaults()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICronService, FakeCronService>();
        services.AddNJobDesk();

        var provider = services.BuildServiceProvider();

        Assert.IsType<FakeCronService>(provider.GetRequiredService<ICronService>());
    }

    [Fact]
    public void First_call_wins_the_section_name()
    {
        var services = new ServiceCollection();
        var first = services.AddNJobDesk("Custom");
        var second = services.AddNJobDesk("Ignored");

        Assert.Equal("Custom", first.SectionName);
        Assert.Equal("Custom", second.SectionName);
    }

    private sealed class FakeCronService : ICronService
    {
        public Core.Models.CronValidationResultModel Validate(string cronExpression, int nextFireTimeCount = 5, string? timeZoneId = null) =>
            new() { IsValid = true };
    }
}

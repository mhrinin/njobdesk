using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Demo.Umbraco.V16;

/// <summary>A demo recurring job: pretends to send queued newsletters every two minutes.</summary>
public sealed class DemoNewsletterDispatchJob(ILogger<DemoNewsletterDispatchJob> logger) : IRecurringBackgroundJob
{
    public TimeSpan Period => TimeSpan.FromMinutes(2);

    public event EventHandler PeriodChanged
    {
        add { }
        remove { }
    }

    public async Task RunJobAsync()
    {
        logger.LogInformation("Dispatching queued newsletters...");
        await Task.Delay(TimeSpan.FromSeconds(3));
        logger.LogInformation("Newsletter dispatch finished.");
    }
}

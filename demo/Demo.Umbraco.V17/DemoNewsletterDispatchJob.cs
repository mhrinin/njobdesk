using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace Demo.Umbraco.V17;

/// <summary>A demo recurring job: pretends to send queued newsletters every two minutes.</summary>
public sealed class DemoNewsletterDispatchJob(ILogger<DemoNewsletterDispatchJob> logger)
    : RecurringBackgroundJobBase(TimeSpan.FromMinutes(2)), ITriggerableRecurringBackgroundJob
{
    public override async Task RunJobAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Dispatching queued newsletters...");
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
        logger.LogInformation("Newsletter dispatch finished.");
    }
}

#if !NET10_0_OR_GREATER
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Store;
using Umbraco.Cms.Infrastructure.BackgroundJobs;

namespace NJobDesk.Umbraco.Providers;

/// <summary>
/// Manual-trigger fallback for Umbraco 16, which has no native trigger API: runs the registered job
/// instance directly (one run per job at a time) and records the run in the execution history.
/// Umbraco 16's <see cref="IRecurringBackgroundJob.RunJobAsync"/> takes no cancellation token, so a
/// run cannot be stopped once started.
/// </summary>
internal sealed class UmbracoJobFallbackRunner(
    IExecutionHistoryWriter historyWriter,
    TimeProvider timeProvider,
    ILogger<UmbracoJobFallbackRunner> logger)
{
    private readonly ConcurrentDictionary<string, byte> _running = new();

    public async Task<bool> TriggerAsync(IRecurringBackgroundJob job, string jobId, CancellationToken cancellationToken)
    {
        if (!_running.TryAdd(jobId, 0))
        {
            return false;
        }

        var fireInstanceId = Guid.NewGuid().ToString("N");
        try
        {
            await historyWriter.StartAsync(
                new JobExecutionHistory
                {
                    FireInstanceId = fireInstanceId,
                    SchedulerInstanceId = Environment.MachineName,
                    ProviderKey = UmbracoRecurringJobsProvider.Key,
                    JobId = jobId,
                    JobName = job.GetType().Name,
                    TriggerId = jobId,
                    TriggerName = $"{job.GetType().Name}-schedule",
                    StartedUtc = timeProvider.GetUtcNow().UtcDateTime,
                    Status = ExecutionStatus.Running,
                },
                cancellationToken);
        }
        catch
        {
            _running.TryRemove(jobId, out _);
            throw;
        }

        _ = RunAsync(job, jobId, fireInstanceId);
        return true;
    }

    private async Task RunAsync(IRecurringBackgroundJob job, string jobId, string fireInstanceId)
    {
        try
        {
            await job.RunJobAsync();
            await historyWriter.CompleteAsync(UmbracoRecurringJobsProvider.Key, fireInstanceId, ExecutionStatus.Succeeded);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Manually triggered job {JobId} failed.", jobId);
            await historyWriter.CompleteAsync(
                UmbracoRecurringJobsProvider.Key, fireInstanceId, ExecutionStatus.Failed, exception.Message);
        }
        finally
        {
            _running.TryRemove(jobId, out _);
        }
    }
}
#endif

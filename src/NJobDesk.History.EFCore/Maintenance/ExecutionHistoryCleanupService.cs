using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJobDesk.Core.Store;
using NJobDesk.History.EFCore.Configuration;

namespace NJobDesk.History.EFCore.Maintenance;

/// <summary>
/// Deletes finished executions past the retention window and fails runs stuck in Running for more
/// than a day, on a plain timer — no scheduler involved, so it works with every provider.
/// </summary>
internal sealed class ExecutionHistoryCleanupService(
    IExecutionHistoryStore historyStore,
    IOptionsMonitor<NJobDeskHistoryOptions> options,
    TimeProvider timeProvider,
    ILogger<ExecutionHistoryCleanupService> logger)
    : BackgroundService
{
    internal const int DeleteBatchSize = 500;

    internal static readonly TimeSpan StaleRunningThreshold = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.CurrentValue.CleanupInterval, timeProvider);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunCleanupAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    internal async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
            var deletedCount = await historyStore.DeleteFinishedBeforeAsync(
                nowUtc.AddDays(-options.CurrentValue.RetentionDays),
                DeleteBatchSize,
                cancellationToken);

            var staleCount = await historyStore.MarkStaleRunningAsFailedAsync(
                nowUtc.Subtract(StaleRunningThreshold),
                schedulerInstanceIdPrefix: null,
                "Marked as failed by cleanup: still recorded as running after 24 hours, node presumed dead.",
                cancellationToken);

            if (deletedCount > 0 || staleCount > 0)
            {
                logger.LogInformation(
                    "Execution history cleanup removed {DeletedCount} entries and reconciled {StaleCount} stale running entries.",
                    deletedCount,
                    staleCount);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Execution history cleanup failed; it will retry on the next interval.");
        }
    }
}

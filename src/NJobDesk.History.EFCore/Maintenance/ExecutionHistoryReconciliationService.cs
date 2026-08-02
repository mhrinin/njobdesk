using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NJobDesk.Core.Store;
using NJobDesk.History.EFCore.Configuration;

namespace NJobDesk.History.EFCore.Maintenance;

/// <summary>
/// Fails history entries left in Running by a previous process at startup — the schedulers were not
/// running, so those runs can no longer finish.
/// </summary>
internal sealed class ExecutionHistoryReconciliationService(
    IExecutionHistoryStore historyStore,
    IOptions<NJobDeskHistoryOptions> options,
    TimeProvider timeProvider,
    ILogger<ExecutionHistoryReconciliationService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var instanceIdPrefix = options.Value.Clustered ? Environment.MachineName : null;
        var reconciledCount = await historyStore.MarkStaleRunningAsFailedAsync(
            timeProvider.GetUtcNow().UtcDateTime,
            instanceIdPrefix,
            "Marked as failed at startup: the scheduler was restarted while the job was recorded as running.",
            cancellationToken);

        if (reconciledCount > 0)
        {
            logger.LogInformation("Reconciled {Count} stale running execution history entries at startup.", reconciledCount);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Store;

public interface IExecutionHistoryStore
{
    Task<PagedResult<JobExecutionHistory>> GetPageAsync(ExecutionHistoryFilter filter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobExecutionHistory>> GetRunningAsync(CancellationToken cancellationToken = default);

    Task<ExecutionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<JobExecutionLog>> GetLogsAsync(long executionId, CancellationToken cancellationToken = default);

    Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default);

    Task<int> MarkStaleRunningAsFailedAsync(
        DateTime startedBeforeUtc,
        string? schedulerInstanceIdPrefix,
        string reason,
        CancellationToken cancellationToken = default);
}

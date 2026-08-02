using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Store;

internal sealed class EmptyExecutionHistoryStore : IExecutionHistoryStore
{
    public Task<PagedResult<JobExecutionHistory>> GetPageAsync(ExecutionHistoryFilter filter, CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<JobExecutionHistory>(0, []));

    public Task<IReadOnlyList<JobExecutionHistory>> GetRunningAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JobExecutionHistory>>([]);

    public Task<ExecutionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new ExecutionStatistics());

    public Task<IReadOnlyList<JobExecutionLog>> GetLogsAsync(long executionId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<JobExecutionLog>>([]);

    public Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<int> MarkStaleRunningAsFailedAsync(
        DateTime startedBeforeUtc,
        string? schedulerInstanceIdPrefix,
        string reason,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);
}

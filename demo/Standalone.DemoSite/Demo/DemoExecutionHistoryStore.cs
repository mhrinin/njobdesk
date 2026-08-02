using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Store;

namespace Standalone.DemoSite.Demo;

internal sealed class DemoExecutionHistoryStore(DemoSchedulerState state, TimeProvider timeProvider) : IExecutionHistoryStore
{
    public Task<PagedResult<JobExecutionHistory>> GetPageAsync(
        ExecutionHistoryFilter filter, CancellationToken cancellationToken = default)
    {
        var (items, _) = state.SnapshotHistory();
        var query = items
            .Where(entry => filter.JobGroup is null || entry.JobGroup == filter.JobGroup)
            .Where(entry => filter.JobName is null || entry.JobName.Contains(filter.JobName, StringComparison.OrdinalIgnoreCase))
            .Where(entry => filter.Status is null || entry.Status == filter.Status)
            .Where(entry => filter.FromUtc is null || entry.StartedUtc >= filter.FromUtc)
            .Where(entry => filter.ToUtc is null || entry.StartedUtc <= filter.ToUtc)
            .OrderByDescending(entry => entry.StartedUtc)
            .ToList();

        return Task.FromResult(new PagedResult<JobExecutionHistory>(
            query.Count,
            query.Skip(filter.Skip).Take(filter.Take).ToList()));
    }

    public Task<IReadOnlyList<JobExecutionHistory>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        var (items, _) = state.SnapshotHistory();
        return Task.FromResult<IReadOnlyList<JobExecutionHistory>>(
            [.. items.Where(entry => entry.Status == ExecutionStatus.Running).OrderBy(entry => entry.StartedUtc)]);
    }

    public Task<ExecutionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var (items, _) = state.SnapshotHistory();
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var dayAgo = now.AddHours(-24);
        var last24h = items.Where(entry => entry.StartedUtc >= dayAgo).ToList();

        var buckets = Enumerable.Range(0, 24)
            .Select(offset =>
            {
                var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc).AddHours(-23 + offset);
                var inHour = last24h.Where(entry => entry.StartedUtc >= hourStart && entry.StartedUtc < hourStart.AddHours(1));
                return new ExecutionBucket
                {
                    HourStartUtc = hourStart,
                    Succeeded = inHour.Count(entry => entry.Status == ExecutionStatus.Succeeded),
                    Failed = inHour.Count(entry => entry.Status == ExecutionStatus.Failed),
                };
            })
            .ToList();

        return Task.FromResult(new ExecutionStatistics
        {
            RunningCount = items.Count(entry => entry.Status == ExecutionStatus.Running),
            Succeeded24h = last24h.Count(entry => entry.Status == ExecutionStatus.Succeeded),
            Failed24h = last24h.Count(entry => entry.Status == ExecutionStatus.Failed),
            Buckets = buckets,
        });
    }

    public Task<IReadOnlyList<JobExecutionLog>> GetLogsAsync(long executionId, CancellationToken cancellationToken = default)
    {
        var (_, logs) = state.SnapshotHistory();
        return Task.FromResult<IReadOnlyList<JobExecutionLog>>(
            logs.TryGetValue(executionId, out var entries) ? entries : []);
    }

    public Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.RemoveFinishedBefore(cutoffUtc, batchSize));

    public Task<int> MarkStaleRunningAsFailedAsync(
        DateTime startedBeforeUtc, string? schedulerInstanceIdPrefix, string reason, CancellationToken cancellationToken = default) =>
        Task.FromResult(state.MarkStaleRunning(startedBeforeUtc, reason));
}

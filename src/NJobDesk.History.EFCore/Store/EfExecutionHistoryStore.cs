using Microsoft.EntityFrameworkCore;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Store;
using NJobDesk.History.EFCore.Persistence;

namespace NJobDesk.History.EFCore.Store;

internal sealed class EfExecutionHistoryStore(
    IDbContextFactory<NJobDeskHistoryDbContext> contextFactory,
    TimeProvider timeProvider)
    : IExecutionHistoryStore
{
    public async Task<PagedResult<JobExecutionHistory>> GetPageAsync(ExecutionHistoryFilter filter, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<JobExecutionHistory> query = context.ExecutionHistory.AsNoTracking();
        if (!string.IsNullOrEmpty(filter.ProviderKey))
        {
            query = query.Where(entry => entry.ProviderKey == filter.ProviderKey);
        }

        if (!string.IsNullOrEmpty(filter.JobId))
        {
            query = query.Where(entry => entry.JobId == filter.JobId);
        }

        if (!string.IsNullOrEmpty(filter.JobName))
        {
            query = query.Where(entry => entry.JobName.Contains(filter.JobName));
        }

        if (filter.Status is { } status)
        {
            query = query.Where(entry => entry.Status == status);
        }

        if (filter.FromUtc is { } fromUtc)
        {
            query = query.Where(entry => entry.StartedUtc >= fromUtc);
        }

        if (filter.ToUtc is { } toUtc)
        {
            query = query.Where(entry => entry.StartedUtc <= toUtc);
        }

        var total = await query.LongCountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(entry => entry.StartedUtc)
            .ThenByDescending(entry => entry.Id)
            .Skip(filter.Skip)
            .Take(filter.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<JobExecutionHistory>(total, items);
    }

    public async Task<IReadOnlyList<JobExecutionHistory>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ExecutionHistory.AsNoTracking()
            .Where(entry => entry.Status == ExecutionStatus.Running)
            .OrderByDescending(entry => entry.StartedUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<ExecutionStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var windowStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, nowUtc.Hour, 0, 0, DateTimeKind.Utc)
            .AddHours(-23);

        var runningCount = await context.ExecutionHistory
            .CountAsync(entry => entry.Status == ExecutionStatus.Running, cancellationToken);

        var finishedCounts = await context.ExecutionHistory
            .Where(entry => entry.StartedUtc >= windowStartUtc
                            && (entry.Status == ExecutionStatus.Succeeded || entry.Status == ExecutionStatus.Failed))
            .GroupBy(entry => new
            {
                entry.Status,
                entry.StartedUtc.Year,
                entry.StartedUtc.Month,
                entry.StartedUtc.Day,
                entry.StartedUtc.Hour,
            })
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(cancellationToken);

        var buckets = Enumerable.Range(0, 24)
            .Select(hour => new ExecutionBucket { HourStartUtc = windowStartUtc.AddHours(hour) })
            .ToArray();

        foreach (var entry in finishedCounts)
        {
            var hourStartUtc = new DateTime(entry.Key.Year, entry.Key.Month, entry.Key.Day, entry.Key.Hour, 0, 0, DateTimeKind.Utc);
            var index = (int)(hourStartUtc - windowStartUtc).TotalHours;
            if (index is < 0 or >= 24)
            {
                continue;
            }

            buckets[index] = entry.Key.Status == ExecutionStatus.Succeeded
                ? buckets[index] with { Succeeded = entry.Count }
                : buckets[index] with { Failed = entry.Count };
        }

        return new ExecutionStatistics
        {
            RunningCount = runningCount,
            Succeeded24h = buckets.Sum(bucket => bucket.Succeeded),
            Failed24h = buckets.Sum(bucket => bucket.Failed),
            Buckets = buckets,
        };
    }

    public async Task<IReadOnlyList<JobExecutionLog>> GetLogsAsync(long executionId, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.ExecutionLogs.AsNoTracking()
            .Where(entry => entry.ExecutionId == executionId)
            .OrderBy(entry => entry.TimestampUtc)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> DeleteFinishedBeforeAsync(DateTime cutoffUtc, int batchSize, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var totalDeleted = 0;
        while (true)
        {
            var batchIds = await context.ExecutionHistory
                .Where(entry => entry.Status != ExecutionStatus.Running && entry.StartedUtc < cutoffUtc)
                .OrderBy(entry => entry.Id)
                .Select(entry => entry.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (batchIds.Count == 0)
            {
                return totalDeleted;
            }

            totalDeleted += await context.ExecutionHistory
                .Where(entry => batchIds.Contains(entry.Id))
                .ExecuteDeleteAsync(cancellationToken);

            if (batchIds.Count < batchSize)
            {
                return totalDeleted;
            }
        }
    }

    public async Task<int> MarkStaleRunningAsFailedAsync(
        DateTime startedBeforeUtc,
        string? schedulerInstanceIdPrefix,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var finishedUtc = timeProvider.GetUtcNow().UtcDateTime;
        var query = context.ExecutionHistory
            .Where(entry => entry.Status == ExecutionStatus.Running && entry.StartedUtc < startedBeforeUtc);
        if (schedulerInstanceIdPrefix is not null)
        {
            query = query.Where(entry => entry.SchedulerInstanceId.StartsWith(schedulerInstanceIdPrefix));
        }

        return await query.ExecuteUpdateAsync(
            setters => setters
                .SetProperty(entry => entry.Status, ExecutionStatus.Failed)
                .SetProperty(entry => entry.FinishedUtc, finishedUtc)
                .SetProperty(entry => entry.ErrorMessage, reason),
            cancellationToken);
    }
}

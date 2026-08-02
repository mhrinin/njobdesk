using Microsoft.EntityFrameworkCore;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Store;
using NJobDesk.History.EFCore.Persistence;

namespace NJobDesk.History.EFCore.Store;

internal sealed class EfExecutionHistoryWriter(
    IDbContextFactory<NJobDeskHistoryDbContext> contextFactory,
    TimeProvider timeProvider)
    : IExecutionHistoryWriter
{
    public async Task StartAsync(JobExecutionHistory entry, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        context.ExecutionHistory.Add(entry);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(
        string providerKey,
        string fireInstanceId,
        ExecutionStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);

        var entry = await context.ExecutionHistory
            .FirstOrDefaultAsync(
                candidate => candidate.ProviderKey == providerKey && candidate.FireInstanceId == fireInstanceId,
                cancellationToken);
        if (entry is null)
        {
            return;
        }

        var finishedUtc = timeProvider.GetUtcNow().UtcDateTime;
        entry.FinishedUtc = finishedUtc;
        entry.DurationMs = (long)(finishedUtc - entry.StartedUtc).TotalMilliseconds;
        entry.Status = status;
        entry.ErrorMessage = errorMessage;
        await context.SaveChangesAsync(cancellationToken);
    }
}

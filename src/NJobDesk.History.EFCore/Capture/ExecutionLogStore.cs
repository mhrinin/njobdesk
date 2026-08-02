using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NJobDesk.Core.Entities;
using NJobDesk.History.EFCore.Persistence;

namespace NJobDesk.History.EFCore.Capture;

internal sealed class ExecutionLogStore(
    IDbContextFactory<NJobDeskHistoryDbContext> contextFactory,
    TimeProvider timeProvider,
    ILogger<ExecutionLogStore> logger)
    : IExecutionLogStore
{
    internal const int MaxCategoryLength = 512;
    internal const int MaxMessageLength = 8000;
    internal const int MaxExceptionLength = 16000;
    internal const int MaxPropertiesLength = 16000;

    public async Task SaveAsync(
        string providerKey,
        string fireInstanceId,
        ExecutionLogCaptureScope scope,
        CancellationToken cancellationToken = default)
    {
        // Capture ends here even if the caller didn't dispose the scope yet — otherwise this store's
        // own database logging would be captured into the buffer it is persisting.
        scope.Dispose();
        var buffer = scope.Buffer;
        if (buffer.IsEmpty)
        {
            return;
        }

        try
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var executionId = await context.ExecutionHistory
                .Where(entry => entry.ProviderKey == providerKey && entry.FireInstanceId == fireInstanceId)
                .Select(entry => (long?)entry.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (executionId is not { } id)
            {
                logger.LogDebug(
                    "No execution history entry found for fire instance {FireInstanceId}; captured logs were discarded.",
                    fireInstanceId);
                return;
            }

            foreach (var entry in buffer.Snapshot())
            {
                entry.ExecutionId = id;
                entry.Category = Truncate(entry.Category, MaxCategoryLength) ?? string.Empty;
                entry.Message = Truncate(entry.Message, MaxMessageLength) ?? string.Empty;
                entry.Exception = Truncate(entry.Exception, MaxExceptionLength);
                entry.Properties = entry.Properties is { Length: > MaxPropertiesLength } ? null : entry.Properties;
                context.ExecutionLogs.Add(entry);
            }

            if (buffer.DroppedCount > 0)
            {
                context.ExecutionLogs.Add(new JobExecutionLog
                {
                    ExecutionId = id,
                    TimestampUtc = timeProvider.GetUtcNow().UtcDateTime,
                    Level = ExecutionLogLevel.Warning,
                    Category = typeof(ExecutionLogStore).FullName!,
                    Message = $"Log capture limit reached; {buffer.DroppedCount} additional entries were discarded.",
                });
            }

            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to persist captured logs for fire instance {FireInstanceId}.", fireInstanceId);
        }
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is { } text && text.Length > maxLength ? text[..maxLength] : value;
}

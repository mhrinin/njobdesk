using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Store;

internal sealed class NullExecutionHistoryWriter : IExecutionHistoryWriter
{
    public Task StartAsync(JobExecutionHistory entry, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task CompleteAsync(
        string providerKey,
        string fireInstanceId,
        ExecutionStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

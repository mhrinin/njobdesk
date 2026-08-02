using NJobDesk.Core.Entities;

namespace NJobDesk.Core.Store;

/// <summary>
/// Write side of execution history. Scheduler providers record run lifecycles through this contract;
/// without a history package the default implementation discards them.
/// </summary>
public interface IExecutionHistoryWriter
{
    /// <summary>
    /// Records the start of a run. The entry carries the provider key, provider-local ids, and a
    /// fire-instance id unique within the provider.
    /// </summary>
    /// <param name="entry">The run entry, with <see cref="JobExecutionHistory.Status"/> Running.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task StartAsync(JobExecutionHistory entry, CancellationToken cancellationToken = default);

    /// <summary>Marks a started run as finished and stamps its duration.</summary>
    /// <param name="providerKey">The provider key the run was recorded under.</param>
    /// <param name="fireInstanceId">The fire-instance id passed to <see cref="StartAsync"/>.</param>
    /// <param name="status">The final status.</param>
    /// <param name="errorMessage">The failure message, when the run failed.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task CompleteAsync(
        string providerKey,
        string fireInstanceId,
        ExecutionStatus status,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}

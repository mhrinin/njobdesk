namespace NJobDesk.History.EFCore.Capture;

/// <summary>Persists a run's captured log buffer against its history entry.</summary>
public interface IExecutionLogStore
{
    /// <summary>
    /// Attaches the scope's captured entries to the history entry identified by the provider key and
    /// fire-instance id. A missing history entry discards the logs; failures are logged, never thrown.
    /// </summary>
    /// <param name="providerKey">The provider key the run was recorded under.</param>
    /// <param name="fireInstanceId">The run's fire-instance id.</param>
    /// <param name="scope">The finished capture scope.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task SaveAsync(
        string providerKey,
        string fireInstanceId,
        ExecutionLogCaptureScope scope,
        CancellationToken cancellationToken = default);
}

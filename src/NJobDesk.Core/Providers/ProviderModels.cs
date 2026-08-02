using NJobDesk.Core.Models;

namespace NJobDesk.Core.Providers;

/// <summary>
/// Stamps provider-local models with their provider key, turning local ids into the
/// <see cref="CompositeId"/> form the dashboard API exposes.
/// </summary>
public static class ProviderModels
{
    /// <summary>Stamps a job summary with its provider key.</summary>
    /// <param name="job">The provider-local job summary.</param>
    /// <param name="providerKey">The provider key.</param>
    public static JobSummaryModel Stamp(this JobSummaryModel job, string providerKey) => job with
    {
        Id = CompositeId.Compose(providerKey, job.Id),
        ProviderKey = providerKey,
    };

    /// <summary>Stamps a trigger with its provider key.</summary>
    /// <param name="trigger">The provider-local trigger.</param>
    /// <param name="providerKey">The provider key.</param>
    public static TriggerModel Stamp(this TriggerModel trigger, string providerKey) => trigger with
    {
        Id = CompositeId.Compose(providerKey, trigger.Id),
    };

    /// <summary>Stamps an execution with its provider key.</summary>
    /// <param name="execution">The provider-local execution.</param>
    /// <param name="providerKey">The provider key.</param>
    public static ExecutionModel Stamp(this ExecutionModel execution, string providerKey) => execution with
    {
        JobId = CompositeId.Compose(providerKey, execution.JobId),
        ProviderKey = providerKey,
    };

    /// <summary>Stamps a job detail (job, triggers, and recent executions) with its provider key.</summary>
    /// <param name="detail">The provider-local job detail.</param>
    /// <param name="providerKey">The provider key.</param>
    public static JobDetailModel Stamp(this JobDetailModel detail, string providerKey) => detail with
    {
        Job = detail.Job.Stamp(providerKey),
        Triggers = [.. detail.Triggers.Select(trigger => trigger.Stamp(providerKey))],
        RecentExecutions = [.. detail.RecentExecutions.Select(execution => execution.Stamp(providerKey))],
    };
}

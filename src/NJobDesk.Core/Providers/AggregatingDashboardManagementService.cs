using Microsoft.Extensions.Logging;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace NJobDesk.Core.Providers;

internal sealed class AggregatingDashboardManagementService(
    ISchedulerProviderRegistry registry,
    ILogger<AggregatingDashboardManagementService> logger) : IDashboardManagementService
{
    public Task<bool> TriggerJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            jobId,
            static capabilities => capabilities.TriggerNow,
            static (management, id, token) => management.TriggerJobAsync(id, token),
            cancellationToken);

    public Task<bool> PauseJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            jobId,
            static capabilities => capabilities.Pause,
            static (management, id, token) => management.PauseJobAsync(id, token),
            cancellationToken);

    public Task<bool> ResumeJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            jobId,
            static capabilities => capabilities.Pause,
            static (management, id, token) => management.ResumeJobAsync(id, token),
            cancellationToken);

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            jobId,
            static capabilities => capabilities.Delete,
            static (management, id, token) => management.DeleteJobAsync(id, token),
            cancellationToken);

    public Task<bool> PauseTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            triggerId,
            static capabilities => capabilities.Pause,
            static (management, id, token) => management.PauseTriggerAsync(id, token),
            cancellationToken);

    public Task<bool> ResumeTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            triggerId,
            static capabilities => capabilities.Pause,
            static (management, id, token) => management.ResumeTriggerAsync(id, token),
            cancellationToken);

    public Task<bool> ResetTriggerFromErrorAsync(string triggerId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            triggerId,
            static capabilities => capabilities.Pause,
            static (management, id, token) => management.ResetTriggerFromErrorAsync(id, token),
            cancellationToken);

    public Task<bool> UnscheduleTriggerAsync(string triggerId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            triggerId,
            static capabilities => capabilities.Delete,
            static (management, id, token) => management.UnscheduleTriggerAsync(id, token),
            cancellationToken);

    public Task PauseAllAsync(CancellationToken cancellationToken = default) =>
        ForEachPausableAsync(static (management, token) => management.PauseAllAsync(token), cancellationToken);

    public Task ResumeAllAsync(CancellationToken cancellationToken = default) =>
        ForEachPausableAsync(static (management, token) => management.ResumeAllAsync(token), cancellationToken);

    public async Task<RescheduleResult> RescheduleAsync(
        string triggerId,
        RescheduleRequestModel request,
        CancellationToken cancellationToken = default)
    {
        if (!CompositeId.TrySplit(triggerId, out var providerKey, out var localId)
            || registry.Find(providerKey) is not { } provider)
        {
            return new RescheduleResult(RescheduleStatus.TriggerNotFound);
        }

        if (!provider.Descriptor.Capabilities.ScheduleEditing)
        {
            return new RescheduleResult(
                RescheduleStatus.NotSupported,
                $"The '{provider.Descriptor.DisplayName}' provider does not support schedule editing.");
        }

        var result = await provider.Management.RescheduleAsync(localId, request, cancellationToken);
        return result.Trigger is { } trigger ? result with { Trigger = trigger.Stamp(providerKey) } : result;
    }

    private async Task<bool> ExecuteAsync(
        string id,
        Func<SchedulerCapabilities, bool> capability,
        Func<ISchedulerManagementService, string, CancellationToken, Task<bool>> action,
        CancellationToken cancellationToken)
    {
        if (!CompositeId.TrySplit(id, out var providerKey, out var localId)
            || registry.Find(providerKey) is not { } provider
            || !capability(provider.Descriptor.Capabilities))
        {
            return false;
        }

        return await action(provider.Management, localId, cancellationToken);
    }

    private async Task ForEachPausableAsync(
        Func<ISchedulerManagementService, CancellationToken, Task> action,
        CancellationToken cancellationToken)
    {
        foreach (var provider in registry.Providers.Where(candidate => candidate.Descriptor.Capabilities.Pause))
        {
            try
            {
                await action(provider.Management, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception, "Scheduler provider '{ProviderKey}' call failed.", provider.Descriptor.Key);
            }
        }
    }
}

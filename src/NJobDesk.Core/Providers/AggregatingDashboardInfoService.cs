using Microsoft.Extensions.Logging;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Providers;

internal sealed class AggregatingDashboardInfoService(
    ISchedulerProviderRegistry registry,
    TimeProvider timeProvider,
    ILogger<AggregatingDashboardInfoService> logger) : IDashboardInfoService
{
    internal static readonly TimeSpan ProviderCallTimeout = TimeSpan.FromSeconds(10);

    public async Task<DashboardStatusModel> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var results = await FanOutAsync(
            static (provider, token) => provider.Info.GetStatusAsync(token), registry.Providers, cancellationToken);

        return new DashboardStatusModel
        {
            Providers = [.. results.Select(result => new ProviderStatusModel
            {
                Key = result.Provider.Descriptor.Key,
                DisplayName = result.Provider.Descriptor.DisplayName,
                ProviderVersion = result.Provider.Descriptor.ProviderVersion,
                Capabilities = result.Provider.Descriptor.Capabilities,
                Degraded = result.Error is not null,
                Error = result.Error?.Message,
                Status = result.Result,
            })],
        };
    }

    public async Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var results = await FanOutAsync(
            static (provider, token) => provider.Info.GetStatisticsAsync(token), registry.Providers, cancellationToken);
        var statistics = results.Where(result => result.Error is null).Select(result => result.Result!).ToList();

        return new SchedulerStatisticsModel
        {
            JobsTotal = statistics.Sum(model => model.JobsTotal),
            JobsPaused = statistics.Sum(model => model.JobsPaused),
            RunningCount = statistics.Sum(model => model.RunningCount),
            Succeeded24h = statistics.Sum(model => model.Succeeded24h),
            Failed24h = statistics.Sum(model => model.Failed24h),
            Buckets = [.. statistics
                .SelectMany(model => model.Buckets)
                .GroupBy(bucket => bucket.HourStartUtc)
                .OrderBy(group => group.Key)
                .Select(group => new ExecutionBucketModel
                {
                    HourStartUtc = group.Key,
                    Succeeded = group.Sum(bucket => bucket.Succeeded),
                    Failed = group.Sum(bucket => bucket.Failed),
                })],
        };
    }

    public async Task<PagedResult<JobSummaryModel>> GetJobsAsync(
        int skip,
        int take,
        string? providerKey = null,
        string? group = null,
        string? filter = null,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ISchedulerProvider> targets = providerKey is null
            ? registry.Providers
            : registry.Find(providerKey) is { } provider ? [provider] : [];
        if (targets.Count == 0)
        {
            return new PagedResult<JobSummaryModel>(0, []);
        }

        var results = await FanOutAsync(
            (target, token) => target.Info.GetJobsAsync(0, skip + take, group, filter, token), targets, cancellationToken);
        var pages = results.Where(result => result.Error is null).ToList();
        var jobs = pages
            .SelectMany(result => result.Result!.Items.Select(job => job.Stamp(result.Provider.Descriptor.Key)))
            .OrderBy(job => job.ProviderKey, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(job => job.Name, StringComparer.OrdinalIgnoreCase)
            .Skip(skip)
            .Take(take);

        return new PagedResult<JobSummaryModel>(pages.Sum(result => result.Result!.Total), jobs.ToList());
    }

    public async Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        if (!CompositeId.TrySplit(jobId, out var providerKey, out var localId)
            || registry.Find(providerKey) is not { } provider)
        {
            return null;
        }

        var detail = await provider.Info.GetJobAsync(localId, cancellationToken);
        return detail?.Stamp(providerKey);
    }

    public async Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default)
    {
        if (!CompositeId.TrySplit(triggerId, out var providerKey, out var localId)
            || registry.Find(providerKey) is not { } provider)
        {
            return null;
        }

        var trigger = await provider.Info.GetTriggerAsync(localId, cancellationToken);
        return trigger?.Stamp(providerKey);
    }

    public async Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default)
    {
        var results = await FanOutAsync(
            static (provider, token) => provider.Info.GetRunningAsync(token), registry.Providers, cancellationToken);

        return [.. results
            .Where(result => result.Error is null)
            .SelectMany(result => result.Result!.Select(execution => execution.Stamp(result.Provider.Descriptor.Key)))
            .OrderBy(execution => execution.StartedUtc)];
    }

    private async Task<IReadOnlyList<ProviderResult<T>>> FanOutAsync<T>(
        Func<ISchedulerProvider, CancellationToken, Task<T>> call,
        IReadOnlyList<ISchedulerProvider> providers,
        CancellationToken cancellationToken)
        where T : class =>
        await Task.WhenAll(providers.Select(provider => CallGuardedAsync(provider, call, cancellationToken)));

    private async Task<ProviderResult<T>> CallGuardedAsync<T>(
        ISchedulerProvider provider,
        Func<ISchedulerProvider, CancellationToken, Task<T>> call,
        CancellationToken cancellationToken)
        where T : class
    {
        using var timeout = new CancellationTokenSource(ProviderCallTimeout, timeProvider);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);
        try
        {
            return new ProviderResult<T>(provider, await call(provider, linked.Token), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Scheduler provider '{ProviderKey}' call failed.", provider.Descriptor.Key);
            return new ProviderResult<T>(provider, null, exception);
        }
    }

    private readonly record struct ProviderResult<T>(ISchedulerProvider Provider, T? Result, Exception? Error)
        where T : class;
}

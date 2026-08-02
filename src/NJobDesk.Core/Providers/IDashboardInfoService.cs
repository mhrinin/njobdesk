using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Providers;

/// <summary>
/// Read side of the dashboard across all registered providers. Ids are dashboard-level
/// (<see cref="CompositeId"/>); fan-out reads isolate provider failures so one broken provider
/// degrades to a status entry instead of failing the whole request.
/// </summary>
public interface IDashboardInfoService
{
    Task<DashboardStatusModel> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<JobSummaryModel>> GetJobsAsync(
        int skip,
        int take,
        string? providerKey = null,
        string? group = null,
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default);
}

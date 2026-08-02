using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

/// <summary>
/// Read side of a scheduler provider. Job and trigger ids are provider-local (see
/// <see cref="Providers.ISchedulerProvider"/>); the dashboard layer prefixes them with the provider
/// key before they reach the API.
/// </summary>
public interface ISchedulerInfoService
{
    Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default);

    Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<JobSummaryModel>> GetJobsAsync(
        int skip,
        int take,
        string? group = null,
        string? filter = null,
        CancellationToken cancellationToken = default);

    Task<JobDetailModel?> GetJobAsync(string jobId, CancellationToken cancellationToken = default);

    Task<TriggerModel?> GetTriggerAsync(string triggerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default);
}

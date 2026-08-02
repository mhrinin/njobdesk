using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

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

    Task<JobDetailModel?> GetJobAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<TriggerModel?> GetTriggerAsync(string group, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default);
}

using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;

namespace NJobDesk.Core.Services;

internal sealed class NotConfiguredSchedulerInfoService : ISchedulerInfoService
{
    public Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchedulerStatusModel { State = SchedulerState.NotConfigured });

    public Task<SchedulerStatisticsModel> GetStatisticsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new SchedulerStatisticsModel());

    public Task<PagedResult<JobSummaryModel>> GetJobsAsync(
        int skip,
        int take,
        string? group = null,
        string? filter = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<JobSummaryModel>(0, []));

    public Task<JobDetailModel?> GetJobAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult<JobDetailModel?>(null);

    public Task<TriggerModel?> GetTriggerAsync(string group, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult<TriggerModel?>(null);

    public Task<IReadOnlyList<ExecutionModel>> GetRunningAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ExecutionModel>>([]);
}

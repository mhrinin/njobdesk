using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;
using NJobDesk.Core.Store;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Executions")]
public class ExecutionsController(IExecutionHistoryStore historyService, IDashboardInfoService infoService)
    : NJobDeskApiControllerBase
{
    [HttpGet("executions")]
    [ProducesResponseType<PagedResult<ExecutionModel>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<ExecutionModel>> GetExecutions(
        CancellationToken cancellationToken,
        int skip = 0,
        int take = 50,
        string? provider = null,
        string? jobId = null,
        string? jobName = null,
        ExecutionStatus? state = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null)
    {
        var providerKey = provider;
        var localJobId = jobId;
        if (jobId is not null && CompositeId.TrySplit(jobId, out var jobProviderKey, out var jobLocalId))
        {
            providerKey = jobProviderKey;
            localJobId = jobLocalId;
        }

        var page = await historyService.GetPageAsync(
            new ExecutionHistoryFilter
            {
                ProviderKey = providerKey,
                JobId = localJobId,
                JobName = jobName,
                Status = state,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Skip = skip,
                Take = take,
            },
            cancellationToken);

        return new PagedResult<ExecutionModel>(page.Total, page.Items.Select(MapDashboardExecution));
    }

    [HttpGet("executions/{id:long}/logs")]
    [ProducesResponseType<IReadOnlyList<ExecutionLogModel>>(StatusCodes.Status200OK)]
    public async Task<IReadOnlyList<ExecutionLogModel>> GetExecutionLogs(long id, CancellationToken cancellationToken)
    {
        var logs = await historyService.GetLogsAsync(id, cancellationToken);
        return logs.Select(ExecutionModelMapper.MapExecutionLog).ToList();
    }

    [HttpGet("executions/running")]
    [ProducesResponseType<IReadOnlyList<ExecutionModel>>(StatusCodes.Status200OK)]
    public Task<IReadOnlyList<ExecutionModel>> GetRunningExecutions(CancellationToken cancellationToken) =>
        infoService.GetRunningAsync(cancellationToken);

    private static ExecutionModel MapDashboardExecution(JobExecutionHistory entry) =>
        ExecutionModelMapper.MapExecution(entry).Stamp(entry.ProviderKey);
}

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Entities;
using NJobDesk.Core.Mapping;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Executions")]
public class ExecutionsController(IExecutionHistoryStore historyService, ISchedulerInfoService infoService)
    : NJobDeskApiControllerBase
{
    [HttpGet("executions")]
    [ProducesResponseType<PagedResult<ExecutionModel>>(StatusCodes.Status200OK)]
    public async Task<PagedResult<ExecutionModel>> GetExecutions(
        CancellationToken cancellationToken,
        int skip = 0,
        int take = 50,
        string? jobGroup = null,
        string? jobName = null,
        ExecutionStatus? state = null,
        DateTime? fromUtc = null,
        DateTime? toUtc = null)
    {
        var page = await historyService.GetPageAsync(
            new ExecutionHistoryFilter
            {
                JobGroup = jobGroup,
                JobName = jobName,
                Status = state,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Skip = skip,
                Take = take,
            },
            cancellationToken);

        return new PagedResult<ExecutionModel>(page.Total, page.Items.Select(ExecutionModelMapper.MapExecution));
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
}

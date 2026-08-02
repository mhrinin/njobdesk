using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NJobDesk.AspNetCore.Configuration;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Scheduler")]
public class SchedulerController(
    ISchedulerInfoService infoService,
    ISchedulerManagementService managementService,
    IOptions<NJobDeskDashboardOptions> options)
    : NJobDeskApiControllerBase
{
    [HttpGet("scheduler")]
    [ProducesResponseType<SchedulerStatusModel>(StatusCodes.Status200OK)]
    public async Task<SchedulerStatusModel> GetSchedulerStatus(CancellationToken cancellationToken) =>
        await GetStatusAsync(cancellationToken);

    [HttpGet("scheduler/statistics")]
    [ProducesResponseType<SchedulerStatisticsModel>(StatusCodes.Status200OK)]
    public Task<SchedulerStatisticsModel> GetSchedulerStatistics(CancellationToken cancellationToken) =>
        infoService.GetStatisticsAsync(cancellationToken);

    [HttpPost("scheduler/pause-all")]
    [ProducesResponseType<SchedulerStatusModel>(StatusCodes.Status200OK)]
    public async Task<SchedulerStatusModel> PauseAll(CancellationToken cancellationToken)
    {
        await managementService.PauseAllAsync(cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    [HttpPost("scheduler/resume-all")]
    [ProducesResponseType<SchedulerStatusModel>(StatusCodes.Status200OK)]
    public async Task<SchedulerStatusModel> ResumeAll(CancellationToken cancellationToken)
    {
        await managementService.ResumeAllAsync(cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    private async Task<SchedulerStatusModel> GetStatusAsync(CancellationToken cancellationToken)
    {
        var status = await infoService.GetStatusAsync(cancellationToken);
        return status with { ReadOnly = options.Value.ReadOnly };
    }
}

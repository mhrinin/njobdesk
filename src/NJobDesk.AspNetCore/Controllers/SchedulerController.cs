using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NJobDesk.AspNetCore.Configuration;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Scheduler")]
public class SchedulerController(
    IDashboardInfoService infoService,
    IDashboardManagementService managementService,
    IOptions<NJobDeskDashboardOptions> options)
    : NJobDeskApiControllerBase
{
    [HttpGet("scheduler")]
    [ProducesResponseType<DashboardStatusModel>(StatusCodes.Status200OK)]
    public async Task<DashboardStatusModel> GetSchedulerStatus(CancellationToken cancellationToken) =>
        await GetStatusAsync(cancellationToken);

    [HttpGet("scheduler/statistics")]
    [ProducesResponseType<SchedulerStatisticsModel>(StatusCodes.Status200OK)]
    public Task<SchedulerStatisticsModel> GetSchedulerStatistics(CancellationToken cancellationToken) =>
        infoService.GetStatisticsAsync(cancellationToken);

    [HttpPost("scheduler/pause-all")]
    [ProducesResponseType<DashboardStatusModel>(StatusCodes.Status200OK)]
    public async Task<DashboardStatusModel> PauseAll(CancellationToken cancellationToken)
    {
        await managementService.PauseAllAsync(cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    [HttpPost("scheduler/resume-all")]
    [ProducesResponseType<DashboardStatusModel>(StatusCodes.Status200OK)]
    public async Task<DashboardStatusModel> ResumeAll(CancellationToken cancellationToken)
    {
        await managementService.ResumeAllAsync(cancellationToken);
        return await GetStatusAsync(cancellationToken);
    }

    private async Task<DashboardStatusModel> GetStatusAsync(CancellationToken cancellationToken)
    {
        var status = await infoService.GetStatusAsync(cancellationToken);
        return status with { ReadOnly = options.Value.ReadOnly };
    }
}

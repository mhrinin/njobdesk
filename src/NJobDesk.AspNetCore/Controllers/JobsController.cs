using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Jobs")]
public class JobsController(ISchedulerInfoService infoService, ISchedulerManagementService managementService)
    : NJobDeskApiControllerBase
{
    [HttpGet("jobs")]
    [ProducesResponseType<PagedResult<JobSummaryModel>>(StatusCodes.Status200OK)]
    public Task<PagedResult<JobSummaryModel>> GetJobs(
        CancellationToken cancellationToken,
        int skip = 0,
        int take = 50,
        string? group = null,
        string? filter = null) =>
        infoService.GetJobsAsync(skip, take, group, filter, cancellationToken);

    [HttpGet("jobs/{group}/{name}")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailModel>> GetJob(string group, string name, CancellationToken cancellationToken) =>
        await infoService.GetJobAsync(group, name, cancellationToken) is { } job ? job : NotFound();

    [HttpPost("jobs/{group}/{name}/trigger")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<JobDetailModel>> TriggerJob(string group, string name, CancellationToken cancellationToken) =>
        ExecuteActionAsync(group, name, managementService.TriggerJobAsync, infoService.GetJobAsync, cancellationToken);

    [HttpPost("jobs/{group}/{name}/pause")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<JobDetailModel>> PauseJob(string group, string name, CancellationToken cancellationToken) =>
        ExecuteActionAsync(group, name, managementService.PauseJobAsync, infoService.GetJobAsync, cancellationToken);

    [HttpPost("jobs/{group}/{name}/resume")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<JobDetailModel>> ResumeJob(string group, string name, CancellationToken cancellationToken) =>
        ExecuteActionAsync(group, name, managementService.ResumeJobAsync, infoService.GetJobAsync, cancellationToken);

    [HttpDelete("jobs/{group}/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob(string group, string name, CancellationToken cancellationToken) =>
        await managementService.DeleteJobAsync(group, name, cancellationToken) ? Ok() : NotFound();
}

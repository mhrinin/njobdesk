using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Jobs")]
public class JobsController(IDashboardInfoService infoService, IDashboardManagementService managementService)
    : NJobDeskApiControllerBase
{
    [HttpGet("jobs")]
    [ProducesResponseType<PagedResult<JobSummaryModel>>(StatusCodes.Status200OK)]
    public Task<PagedResult<JobSummaryModel>> GetJobs(
        CancellationToken cancellationToken,
        int skip = 0,
        int take = 50,
        string? provider = null,
        string? group = null,
        string? filter = null) =>
        infoService.GetJobsAsync(skip, take, provider, group, filter, cancellationToken);

    [HttpGet("jobs/{id}")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JobDetailModel>> GetJob(string id, CancellationToken cancellationToken) =>
        await infoService.GetJobAsync(id, cancellationToken) is { } job ? job : NotFound();

    [HttpPost("jobs/{id}/trigger")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<JobDetailModel>> TriggerJob(string id, CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, managementService.TriggerJobAsync, infoService.GetJobAsync, cancellationToken);

    [HttpPost("jobs/{id}/pause")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<JobDetailModel>> PauseJob(string id, CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, managementService.PauseJobAsync, infoService.GetJobAsync, cancellationToken);

    [HttpPost("jobs/{id}/resume")]
    [ProducesResponseType<JobDetailModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<JobDetailModel>> ResumeJob(string id, CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, managementService.ResumeJobAsync, infoService.GetJobAsync, cancellationToken);

    [HttpDelete("jobs/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJob(string id, CancellationToken cancellationToken) =>
        await managementService.DeleteJobAsync(id, cancellationToken) ? Ok() : NotFound();
}

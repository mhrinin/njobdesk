using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Triggers")]
public class TriggersController(ISchedulerInfoService infoService, ISchedulerManagementService managementService)
    : NJobDeskApiControllerBase
{
    [HttpPost("triggers/{group}/{name}/pause")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TriggerModel>> PauseTrigger(string group, string name, CancellationToken cancellationToken) =>
        ExecuteActionAsync(group, name, managementService.PauseTriggerAsync, infoService.GetTriggerAsync, cancellationToken);

    [HttpPost("triggers/{group}/{name}/resume")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TriggerModel>> ResumeTrigger(string group, string name, CancellationToken cancellationToken) =>
        ExecuteActionAsync(group, name, managementService.ResumeTriggerAsync, infoService.GetTriggerAsync, cancellationToken);

    [HttpPost("triggers/{group}/{name}/reset-error")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TriggerModel>> ResetTriggerError(string group, string name, CancellationToken cancellationToken) =>
        ExecuteActionAsync(group, name, managementService.ResetTriggerFromErrorAsync, infoService.GetTriggerAsync, cancellationToken);

    [HttpDelete("triggers/{group}/{name}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnscheduleTrigger(string group, string name, CancellationToken cancellationToken) =>
        await managementService.UnscheduleTriggerAsync(group, name, cancellationToken) ? Ok() : NotFound();

    [HttpPut("triggers/{group}/{name}/schedule")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TriggerModel>> RescheduleTrigger(
        string group,
        string name,
        RescheduleRequestModel request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.RescheduleAsync(group, name, request, cancellationToken);
        return result.Status switch
        {
            RescheduleStatus.Success => result.Trigger!,
            RescheduleStatus.TriggerNotFound => NotFound(),
            _ => BadRequest(new ProblemDetails
            {
                Title = "The trigger schedule could not be updated.",
                Detail = result.Error,
                Status = StatusCodes.Status400BadRequest,
            }),
        };
    }
}

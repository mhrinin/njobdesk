using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.Core.Contracts;
using NJobDesk.Core.Models;
using NJobDesk.Core.Providers;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Triggers")]
public class TriggersController(IDashboardInfoService infoService, IDashboardManagementService managementService)
    : NJobDeskApiControllerBase
{
    [HttpPost("triggers/{id}/pause")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TriggerModel>> PauseTrigger(string id, CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, managementService.PauseTriggerAsync, infoService.GetTriggerAsync, cancellationToken);

    [HttpPost("triggers/{id}/resume")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TriggerModel>> ResumeTrigger(string id, CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, managementService.ResumeTriggerAsync, infoService.GetTriggerAsync, cancellationToken);

    [HttpPost("triggers/{id}/reset-error")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<ActionResult<TriggerModel>> ResetTriggerError(string id, CancellationToken cancellationToken) =>
        ExecuteActionAsync(id, managementService.ResetTriggerFromErrorAsync, infoService.GetTriggerAsync, cancellationToken);

    [HttpDelete("triggers/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnscheduleTrigger(string id, CancellationToken cancellationToken) =>
        await managementService.UnscheduleTriggerAsync(id, cancellationToken) ? Ok() : NotFound();

    [HttpPut("triggers/{id}/schedule")]
    [ProducesResponseType<TriggerModel>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TriggerModel>> RescheduleTrigger(
        string id,
        RescheduleRequestModel request,
        CancellationToken cancellationToken)
    {
        var result = await managementService.RescheduleAsync(id, request, cancellationToken);
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

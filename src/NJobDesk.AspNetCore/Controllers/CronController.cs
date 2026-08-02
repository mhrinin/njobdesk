using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.Core.Models;
using NJobDesk.Core.Services;

namespace NJobDesk.AspNetCore.Controllers;

[ApiExplorerSettings(GroupName = "Cron")]
public class CronController(ICronService cronService) : NJobDeskApiControllerBase
{
    [HttpPost("cron/validate")]
    [ProducesResponseType<CronValidationResultModel>(StatusCodes.Status200OK)]
    public CronValidationResultModel ValidateCron(CronValidationRequestModel request) =>
        cronService.Validate(request.CronExpression, request.NextFireTimeCount, request.TimeZoneId);
}

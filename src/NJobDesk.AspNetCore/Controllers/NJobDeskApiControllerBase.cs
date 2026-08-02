using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NJobDesk.AspNetCore.Authorization;

namespace NJobDesk.AspNetCore.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = NJobDeskAuthorization.PolicyName)]
[Produces("application/json")]
public abstract class NJobDeskApiControllerBase : ControllerBase
{
    private protected async Task<ActionResult<TModel>> ExecuteActionAsync<TModel>(
        string id,
        Func<string, CancellationToken, Task<bool>> action,
        Func<string, CancellationToken, Task<TModel?>> fetch,
        CancellationToken cancellationToken)
        where TModel : class
    {
        if (!await action(id, cancellationToken))
        {
            return NotFound();
        }

        return await fetch(id, cancellationToken) is { } model ? model : NotFound();
    }
}

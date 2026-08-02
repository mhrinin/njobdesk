using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using NJobDesk.AspNetCore.Configuration;

namespace NJobDesk.AspNetCore.Hosting;

/// <summary>
/// Blocks mutating requests (POST/PUT/DELETE/PATCH) to the dashboard controllers with 403 when
/// <see cref="NJobDeskDashboardOptions.ReadOnly"/> is enabled. Applied only to the dashboard
/// controllers via the route-prefix convention. GET is always allowed.
/// </summary>
internal sealed class NJobDeskReadOnlyActionFilter(IOptions<NJobDeskDashboardOptions> options) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (options.Value.ReadOnly && !HttpMethods.IsGet(context.HttpContext.Request.Method))
        {
            context.Result = new ObjectResult(new ProblemDetails
            {
                Title = "The NJobDesk dashboard is running in read-only mode.",
                Status = StatusCodes.Status403Forbidden,
            })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
            return;
        }

        await next();
    }
}

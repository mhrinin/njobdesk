using Microsoft.AspNetCore.Http;
using NJobDesk.AspNetCore.Configuration;

namespace NJobDesk.AspNetCore.Authorization;

/// <summary>
/// A per-request authorization hook for the dashboard, evaluated ahead of the configured
/// <see cref="NJobDeskDashboardOptions.AuthorizationPolicy"/>. Mirrors the shape of the native
/// NJobDesk.Dashboard <c>IDashboardAuthorizationFilter</c> so a future migration is mechanical.
/// </summary>
public interface INJobDeskAuthorizationFilter
{
    ValueTask<bool> AuthorizeAsync(HttpContext httpContext);
}

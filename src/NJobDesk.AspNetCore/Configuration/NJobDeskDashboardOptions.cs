using NJobDesk.AspNetCore.Authorization;
namespace NJobDesk.AspNetCore.Configuration;

/// <summary>
/// Web-hosting options for the dashboard. Scheduler/history settings live in the provider and
/// feature packages; these options only shape hosting, routing, and authorization.
/// </summary>
public sealed class NJobDeskDashboardOptions
{
    /// <summary>Base path the dashboard UI is served from.</summary>
    public string DashboardPath { get; set; } = "/njobdesk";

    /// <summary>
    /// Base path for the management API. Unlike the native flat <c>/njd-api</c>, this is an MVC
    /// route template and may carry the <c>{version:apiVersion}</c> token.
    /// </summary>
    public string ApiPath { get; set; } = "njobdesk/api/v{version:apiVersion}";

    /// <summary>
    /// A named authorization policy to guard the dashboard. When set (and not the built-in
    /// <see cref="NJobDeskAuthorization.PolicyName"/>), it is evaluated for every request.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Authentication schemes the dashboard policy authenticates with. Set this when dashboard
    /// requests carry credentials the host's default scheme cannot authenticate (for example a
    /// bearer scheme in a host whose default is a cookie); empty means the host's default scheme.
    /// </summary>
    public string[] AuthenticationSchemes { get; set; } = [];

    /// <summary>A per-request authorization hook, evaluated ahead of <see cref="AuthorizationPolicy"/>.</summary>
    public INJobDeskAuthorizationFilter? AuthorizationFilter { get; set; }

    /// <summary>When true, mutating actions are blocked server-side (403) and hidden in the UI.</summary>
    public bool ReadOnly { get; set; }
}

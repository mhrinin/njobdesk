using Microsoft.Extensions.DependencyInjection;
using NJobDesk.AspNetCore.Configuration;
using NJobDesk.AspNetCore.DependencyInjection;
using NJobDesk.Core.DependencyInjection;

namespace NJobDesk.Hangfire;

/// <summary>One-call NJobDesk setup for Hangfire hosts.</summary>
public static class NJobDeskHangfireExtensions
{
    /// <summary>
    /// Registers the NJobDesk dashboard API together with the Hangfire provider — the only
    /// registration a plain ASP.NET Core + Hangfire host needs besides <c>MapNJobDesk()</c>.
    /// Returns the builder for chaining feature packages (e.g. <c>AddEfHistory</c>).
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional dashboard options (auth, route prefix, read-only mode).</param>
    public static NJobDeskBuilder AddNJobDeskHangfire(
        this IServiceCollection services,
        Action<NJobDeskDashboardOptions>? configure = null) =>
        services.AddNJobDeskApi(configure).AddProvider<HangfireSchedulerProvider>();
}

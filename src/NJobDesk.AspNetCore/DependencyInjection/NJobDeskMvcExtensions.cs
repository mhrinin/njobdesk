using System.Net;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NJobDesk.AspNetCore.Authorization;
using NJobDesk.AspNetCore.Configuration;
using NJobDesk.AspNetCore.Controllers;
using NJobDesk.AspNetCore.Hosting;
using NJobDesk.Core.DependencyInjection;

namespace NJobDesk.AspNetCore.DependencyInjection;

public static class NJobDeskMvcExtensions
{
    /// <summary>
    /// Registers the dashboard API controllers, route prefix, and authorization plus API versioning
    /// for plain ASP.NET Core hosts. Hosts that already configure API versioning should use
    /// <see cref="AddNJobDeskControllers"/> instead.
    /// </summary>
    public static NJobDeskBuilder AddNJobDeskApi(
        this IServiceCollection services,
        Action<NJobDeskDashboardOptions>? configure = null)
    {
        var builder = services.AddNJobDeskControllers(configure);
        services
            .AddApiVersioning(versioning =>
            {
                versioning.AssumeDefaultVersionWhenUnspecified = true;
                versioning.DefaultApiVersion = new ApiVersion(1, 0);
            })
            .AddMvc();
        return builder;
    }

    /// <summary>
    /// Registers the dashboard API controllers with the route-prefix + read-only conventions and the
    /// <see cref="NJobDeskAuthorization.PolicyName"/> policy (which honours the configured
    /// <see cref="NJobDeskDashboardOptions.AuthorizationFilter"/> / <see cref="NJobDeskDashboardOptions.AuthorizationPolicy"/>,
    /// falling back to local requests only), without touching the host's API-versioning configuration.
    /// </summary>
    public static NJobDeskBuilder AddNJobDeskControllers(
        this IServiceCollection services,
        Action<NJobDeskDashboardOptions>? configure = null)
    {
        var builder = services.AddNJobDesk();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddOptions<NJobDeskDashboardOptions>();
        services.TryAddSingleton<NJobDeskDashboardAssets>();
        services.TryAddSingleton<NJobDeskReadOnlyActionFilter>();
        services.AddSingleton<IConfigureOptions<MvcOptions>, ConfigureNJobDeskMvcOptions>();
        services.AddSingleton<IConfigureOptions<AuthorizationOptions>, ConfigureNJobDeskAuthorization>();
        services.AddControllers().AddApplicationPart(typeof(NJobDeskApiControllerBase).Assembly);
        return builder;
    }

    private static bool IsLocalRequest(HttpContext context)
    {
        var connection = context.Connection;
        return connection.RemoteIpAddress is null
            ? connection.LocalIpAddress is null
            : IPAddress.IsLoopback(connection.RemoteIpAddress) || connection.RemoteIpAddress.Equals(connection.LocalIpAddress);
    }

    // Registers the single "NJobDesk" policy (unless the host already defined it) that centralizes the
    // authorization precedence: a configured AuthorizationFilter wins, else a configured
    // AuthorizationPolicy is delegated to, else local requests only. Both the API ([Authorize] on the
    // controller base) and the UI endpoints (MapNJobDesk) reference this one policy.
    private sealed class ConfigureNJobDeskAuthorization(IOptions<NJobDeskDashboardOptions> dashboardOptions)
        : IConfigureOptions<AuthorizationOptions>
    {
        public void Configure(AuthorizationOptions authorization)
        {
            if (authorization.GetPolicy(NJobDeskAuthorization.PolicyName) is not null)
            {
                return;
            }

            authorization.AddPolicy(NJobDeskAuthorization.PolicyName, policy =>
            {
                if (dashboardOptions.Value.AuthenticationSchemes is { Length: > 0 } schemes)
                {
                    policy.AddAuthenticationSchemes(schemes);
                }

                policy.RequireAssertion(async context =>
                {
                    if (context.Resource is not HttpContext httpContext)
                    {
                        return false;
                    }

                    var options = httpContext.RequestServices.GetRequiredService<IOptions<NJobDeskDashboardOptions>>().Value;

                    if (options.AuthorizationFilter is { } filter)
                    {
                        return await filter.AuthorizeAsync(httpContext);
                    }

                    if (!string.IsNullOrEmpty(options.AuthorizationPolicy)
                        && options.AuthorizationPolicy != NJobDeskAuthorization.PolicyName)
                    {
                        var authorizationService = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
                        var result = await authorizationService.AuthorizeAsync(httpContext.User, httpContext, options.AuthorizationPolicy);
                        return result.Succeeded;
                    }

                    return IsLocalRequest(httpContext);
                });
            });
        }
    }

    private sealed class ConfigureNJobDeskMvcOptions(IOptions<NJobDeskDashboardOptions> options)
        : IConfigureOptions<MvcOptions>
    {
        public void Configure(MvcOptions mvc) =>
            mvc.Conventions.Add(new NJobDeskControllerConvention(options.Value.ApiPath));
    }
}

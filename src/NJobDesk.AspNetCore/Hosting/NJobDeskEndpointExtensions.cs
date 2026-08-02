using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NJobDesk.AspNetCore.Authorization;
using NJobDesk.AspNetCore.Configuration;

namespace NJobDesk.AspNetCore.Hosting;

public static class NJobDeskEndpointExtensions
{
    /// <summary>
    /// Serves the dashboard SPA under <paramref name="pattern"/> (defaulting to
    /// <see cref="NJobDeskDashboardOptions.DashboardPath"/>), guarded by
    /// <see cref="RequireNJobDeskAuthorization{TBuilder}"/>.
    /// </summary>
    public static IEndpointConventionBuilder MapNJobDesk(
        this IEndpointRouteBuilder endpoints,
        string? pattern = null)
    {
        pattern ??= endpoints.ServiceProvider
            .GetRequiredService<IOptions<NJobDeskDashboardOptions>>().Value.DashboardPath;
        var basePath = "/" + pattern.Trim('/');

        var group = endpoints.MapGroup(basePath).RequireNJobDeskAuthorization();
        group.MapGet("/", ServeIndex).WithName("NJobDeskDashboard").ExcludeFromDescription();
        group.MapGet("/{**path}", ServeAsset).WithName("NJobDeskDashboardAsset").ExcludeFromDescription();
        return group;
    }

    private static IResult ServeIndex(HttpContext context, NJobDeskDashboardAssets assets) =>
        assets.IndexResult(context);

    private static IResult ServeAsset(string path, HttpContext context, NJobDeskDashboardAssets assets) =>
        assets.AssetResult(path, context);

    /// <summary>
    /// Guards the endpoints with the <see cref="NJobDeskAuthorization.PolicyName"/> policy via an
    /// endpoint filter, so a denied request gets a clean 401 even when the host configured no
    /// authentication scheme (the authorization middleware would otherwise try to challenge and throw).
    /// <see cref="MapNJobDesk"/> already applies this.
    /// </summary>
    public static TBuilder RequireNJobDeskAuthorization<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder =>
        builder.AddEndpointFilter<TBuilder, NJobDeskAuthorizationEndpointFilter>();
}

internal sealed class NJobDeskAuthorizationEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var authorization = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();
        var result = await authorization.AuthorizeAsync(httpContext.User, httpContext, NJobDeskAuthorization.PolicyName);
        return result.Succeeded ? await next(context) : Results.Unauthorized();
    }
}

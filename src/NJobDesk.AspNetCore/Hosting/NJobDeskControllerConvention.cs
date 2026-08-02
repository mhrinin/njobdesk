using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using NJobDesk.AspNetCore.Controllers;

namespace NJobDesk.AspNetCore.Hosting;

/// <summary>
/// Decorates the dashboard controllers: applies the configurable API route prefix and the read-only
/// action filter. Scoped to <see cref="NJobDeskApiControllerBase"/> so the host's own controllers are
/// untouched.
/// </summary>
internal sealed class NJobDeskControllerConvention(string apiPath) : IControllerModelConvention
{
    private readonly AttributeRouteModel prefix = new(new RouteAttribute(apiPath));

    public void Apply(ControllerModel controller)
    {
        if (!typeof(NJobDeskApiControllerBase).IsAssignableFrom(controller.ControllerType))
        {
            return;
        }

        controller.Filters.Add(new ServiceFilterAttribute(typeof(NJobDeskReadOnlyActionFilter)));

        foreach (var selector in controller.Selectors)
        {
            selector.AttributeRouteModel = selector.AttributeRouteModel is null
                ? prefix
                : AttributeRouteModel.CombineAttributeRouteModel(prefix, selector.AttributeRouteModel);
        }
    }
}

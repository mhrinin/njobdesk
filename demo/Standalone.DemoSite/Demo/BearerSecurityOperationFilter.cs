using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Standalone.DemoSite.Demo;

/// <summary>
/// Marks every operation as bearer-secured so the generated TypeScript client carries the security
/// metadata that makes it attach the host's access token (e.g. the Umbraco backoffice token).
/// </summary>
internal sealed class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context) =>
        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "bearer" },
                }] = [],
            },
        ];
}

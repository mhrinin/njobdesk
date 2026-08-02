using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Standalone.DemoSite.Demo;

/// <summary>
/// Marks every non-nullable property as required so the generated TypeScript client gets
/// non-optional members for values the API always returns (matching the C# nullability).
/// </summary>
internal sealed class RequireNonNullableSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        foreach (var property in schema.Properties)
        {
            if (!property.Value.Nullable && !schema.Required.Contains(property.Key))
            {
                schema.Required.Add(property.Key);
            }
        }
    }
}

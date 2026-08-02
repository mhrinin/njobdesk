using Microsoft.OpenApi.Writers;
using NJobDesk.AspNetCore.DependencyInjection;
using NJobDesk.AspNetCore.Hosting;
using NJobDesk.Core.Services;
using NJobDesk.Core.Store;
using Standalone.DemoSite.Demo;
using Swashbuckle.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Demo provider registrations must precede AddNJobDeskApi so its TryAdd defaults yield.
builder.Services.AddSingleton<DemoSchedulerState>();
builder.Services.AddSingleton<ISchedulerInfoService, DemoSchedulerInfoService>();
builder.Services.AddSingleton<ISchedulerManagementService, DemoSchedulerManagementService>();
builder.Services.AddSingleton<IExecutionHistoryStore, DemoExecutionHistoryStore>();

builder.Services
    .AddNJobDeskApi()
    .Services
    .AddApiVersioning()
    .AddApiExplorer(explorer =>
    {
        explorer.GroupNameFormat = "'v'V";
        explorer.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(swagger =>
{
    // The dashboard controllers use ApiExplorer group names per area (Jobs/Cron/...); collect
    // them all into one document.
    swagger.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "NJobDesk API", Version = "v1" });
    swagger.DocInclusionPredicate((_, _) => true);
    swagger.SupportNonNullableReferenceTypes();
    swagger.SchemaFilter<RequireNonNullableSchemaFilter>();
    swagger.CustomOperationIds(api =>
        api.ActionDescriptor is Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor descriptor
            ? char.ToLowerInvariant(descriptor.ActionName[0]) + descriptor.ActionName[1..]
            : null);
});

var app = builder.Build();

// Offline OpenAPI export used by the client's generate-client script:
//   dotnet run -- --export-openapi ../../src/NJobDesk.AspNetCore/openapi/openapi.json
var exportIndex = Array.IndexOf(args, "--export-openapi");
if (exportIndex >= 0)
{
    var outputPath = args.Length > exportIndex + 1 ? args[exportIndex + 1] : "openapi.json";
    var document = app.Services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
    await using var file = File.CreateText(outputPath);
    document.SerializeAsV3(new OpenApiJsonWriter(file));
    Console.WriteLine($"OpenAPI spec written to {Path.GetFullPath(outputPath)}");
    return;
}

app.UseSwagger();
app.MapControllers();
app.MapNJobDesk();
app.MapGet("/", () => Results.Redirect("/njobdesk"));

app.Run();

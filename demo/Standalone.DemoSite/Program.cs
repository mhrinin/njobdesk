using Microsoft.OpenApi.Writers;
using NJobDesk.AspNetCore.DependencyInjection;
using NJobDesk.AspNetCore.Hosting;
using NJobDesk.History.EFCore;
using NJobDesk.History.EFCore.DependencyInjection;
using Standalone.DemoSite.Demo;
using Swashbuckle.AspNetCore.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<DemoSchedulerState>();

builder.Services
    .AddNJobDeskApi()
    .AddEfHistory(HistoryDatabase.Sqlite("Data Source=demo-history.db"))
    .AddProvider<DemoSchedulerProvider>()
    .AddProvider<BasicSchedulerProvider>()
    .AddProvider<FlakySchedulerProvider>()
    .Services
    // Hosted services run in registration order: the seeder needs the schema migrated and the
    // startup reconciliation done before it decides whether to seed.
    .AddHostedService<DemoHistorySeeder>()
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
    // Declared on every operation so the generated client attaches the host's bearer token when one
    // is configured (the Umbraco backoffice); hosts without an auth callback simply omit the header.
    swagger.AddSecurityDefinition("bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
    });
    swagger.OperationFilter<BearerSecurityOperationFilter>();
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

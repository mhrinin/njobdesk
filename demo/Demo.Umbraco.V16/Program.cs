using Demo.Fakes;
using NJobDesk.Core.Providers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// The |DataDirectory| SQLite connection fails on first boot unless the folder already exists.
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data"));

builder.Services.AddSingleton<ISchedulerProvider, FakeSchedulerProvider>();

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();

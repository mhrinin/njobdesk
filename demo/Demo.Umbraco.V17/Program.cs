using Demo.Fakes;
using Demo.Umbraco.V17;
using NJobDesk.Core.DependencyInjection;
using NJobDesk.Core.Providers;
using NJobDesk.History.EFCore;
using NJobDesk.History.EFCore.DependencyInjection;
using NJobDesk.Umbraco.Providers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// The |DataDirectory| SQLite connection fails on first boot unless the folder already exists.
var dataDirectory = Directory
    .CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "umbraco", "Data")).FullName;

builder.Services.AddSingleton<ISchedulerProvider, FakeSchedulerProvider>();
builder.Services.AddRecurringBackgroundJob<DemoNewsletterDispatchJob>();
builder.Services
    .AddNJobDesk()
    .AddEfHistory(HistoryDatabase.Sqlite($"Data Source={Path.Combine(dataDirectory, "njobdesk-history.db")}"));

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddNJobDeskUmbracoJobs()
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

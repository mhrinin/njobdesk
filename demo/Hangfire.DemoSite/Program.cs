using Hangfire;
using Hangfire.DemoSite;
using NJobDesk.AspNetCore.Hosting;
using NJobDesk.Hangfire;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHangfire(configuration => configuration.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddNJobDeskHangfire();

var app = builder.Build();

app.MapControllers();
app.MapNJobDesk();
app.MapGet("/", () => Results.Redirect("/njobdesk"));

var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate("newsletter-digest", () => DemoJobs.SendNewsletterDigest(), "*/5 * * * *");
recurringJobs.AddOrUpdate("cache-refresh", () => DemoJobs.RefreshCache(), "* * * * *");
recurringJobs.AddOrUpdate("flaky-report", () => DemoJobs.BuildFlakyReport(), "*/2 * * * *");

app.Run();

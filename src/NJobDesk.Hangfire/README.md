# NJobDesk.Hangfire

Hangfire provider for **NJobDesk** — a scheduler-agnostic jobs dashboard for .NET.

Lists Hangfire's recurring jobs with their cron schedules and last/next runs (read through `JobStorage`, so any Hangfire storage works), and triggers or removes them from the dashboard. History is last-execution only: each job shows its most recent run.

```csharp
builder.Services.AddHangfire(config => config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services
    .AddNJobDeskApi()
    .AddProvider<HangfireSchedulerProvider>();
```

Combine with other NJobDesk providers (Umbraco recurring jobs, Quartz via uQuartz) on one dashboard.

MIT licensed. https://github.com/mhrinin/njobdesk

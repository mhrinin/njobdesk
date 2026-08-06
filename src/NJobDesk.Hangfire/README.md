# NJobDesk.Hangfire

Hangfire provider for **NJobDesk** — a scheduler-agnostic jobs dashboard for .NET.

Lists Hangfire's recurring jobs with their cron schedules and last/next runs (read through `JobStorage`, so any Hangfire storage works), and triggers or removes them from the dashboard. History is last-execution only: each job shows its most recent run.

This package brings the NJobDesk dashboard with it — it is the only NJobDesk package a Hangfire host needs.

```csharp
builder.Services.AddHangfire(config => config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services.AddNJobDeskHangfire();

var app = builder.Build();
app.MapControllers();
app.MapNJobDesk();   // dashboard at /njobdesk
```

Hosting the dashboard elsewhere (e.g. inside Umbraco via `NJobDesk.Umbraco`)? Register just the provider instead: `AddNJobDesk().AddProvider<HangfireSchedulerProvider>()`.

Combine with other NJobDesk providers (Umbraco recurring jobs, Quartz via uQuartz) on one dashboard.

MIT licensed. https://github.com/mhrinin/njobdesk

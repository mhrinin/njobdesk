# NJobDesk.Core

Core contracts for **NJobDesk** — a scheduler-agnostic jobs dashboard for .NET.

This package holds the abstractions a scheduler provider implements (`ISchedulerInfoService`, `ISchedulerManagementService`, `IExecutionHistoryStore`, `ICronService`), the API models, and scheduler-agnostic defaults (Cronos-based cron validation, not-configured fallbacks). It has no HTTP surface and no scheduler dependency.

- Install **NJobDesk.AspNetCore** for the dashboard API + embedded UI.
- Install a provider for your scheduler: Quartz (via uQuartz), Hangfire, or Umbraco recurring jobs.

```csharp
builder.Services.AddNJobDesk();
```

MIT licensed. https://github.com/mhrinin/njobdesk

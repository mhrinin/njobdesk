# NJobDesk.Core

Core contracts for **NJobDesk** — a scheduler-agnostic jobs dashboard for .NET.

This package holds the scheduler provider abstraction (`ISchedulerProvider` with a descriptor, capability flags, and provider-local ids), the contracts a provider implements (`ISchedulerInfoService`, `ISchedulerManagementService`, `IExecutionHistoryStore`, `ICronService`), the API models, and the multi-provider aggregation layer (registry, fault isolation, capability enforcement). It has no HTTP surface and no scheduler dependency.

- Install **NJobDesk.AspNetCore** for the dashboard API + embedded UI.
- Install a provider for your scheduler: Quartz (via uQuartz), Hangfire, or Umbraco recurring jobs.

```csharp
builder.Services
    .AddNJobDesk()
    .AddProvider<MySchedulerProvider>();
```

MIT licensed. https://github.com/mhrinin/njobdesk

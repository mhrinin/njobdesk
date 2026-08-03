# NJobDesk

**A scheduler-agnostic jobs dashboard for .NET** — one professionally designed management UI for background jobs across Quartz.NET (via [uQuartz](https://github.com/mhrinin/uQuartz)), Hangfire, and Umbraco recurring jobs, hosted either as plain ASP.NET Core middleware (like Hangfire's dashboard) or inside the Umbraco backoffice. Several schedulers can share one dashboard: jobs are tagged per provider, actions are capability-gated, and a broken provider degrades to a banner instead of a blank screen.

> Extracted from [uQuartz](https://github.com/mhrinin/uQuartz), whose dashboard was already provider-neutral; uQuartz now consumes NJobDesk as its Quartz provider.

- **Overview** — job/run KPI tiles, a 24-hour trend chart, a live "running now" table, and per-provider scheduler cards.
- **Jobs** — state chips, human-readable schedules ("Every 15 minutes", "At 03:00, only on Monday"), next/last fire times, trigger drill-in, and capability-gated actions: run now, pause/resume, delete, cron editing with live validation and next-fire previews.
- **History** — persistent execution history (duration, outcome, error, node) with filtering, plus per-run `ILogger` capture with structured properties and stack traces.
- **Built with [@umbraco-ui/uui](https://github.com/umbraco/Umbraco.UI)** web components — dark mode, colorblind-safe state tags, designed empty states.

## Packages

| Package | Purpose |
|---|---|
| `NJobDesk.Core` | The scheduler-provider abstraction (descriptor, capabilities, registry, multi-provider aggregation with fault isolation), contracts and models. net8.0+. |
| `NJobDesk.AspNetCore` | Management API + embedded dashboard UI for any ASP.NET Core app (`AddNJobDeskApi` / `MapNJobDesk`), with a loopback-only fallback authorization policy, pluggable filters, and a read-only mode. |
| `NJobDesk.History.EFCore` | Persistent execution history + per-run `ILogger` capture on SQL Server or SQLite, with startup migrations, retention cleanup, and stale-run reconciliation. |
| `NJobDesk.Umbraco` | Umbraco 16/17 backoffice integration (Settings → Jobs) + the native `IRecurringBackgroundJob` provider. |
| `NJobDesk.Hangfire` | Hangfire recurring-jobs provider (list, trigger, remove; last-execution history). |
| [`uQuartz`](https://github.com/mhrinin/uQuartz) | Quartz.NET provider with Quartz-exact cron validation, full history + log capture, and job-store provisioning. |

## Quick start — plain ASP.NET Core (Hangfire example)

```csharp
builder.Services.AddHangfire(config => config.UseInMemoryStorage());
builder.Services.AddHangfireServer();

builder.Services
    .AddNJobDeskApi()
    .AddProvider<HangfireSchedulerProvider>()
    .AddEfHistory(HistoryDatabase.Sqlite("Data Source=njobdesk-history.db")); // optional

var app = builder.Build();
app.MapControllers();
app.MapNJobDesk();   // dashboard at /njobdesk
```

Without authentication configured, the dashboard's fallback policy allows local requests only; wire `NJobDeskDashboardOptions.AuthorizationPolicy` or `AuthorizationFilter` for anything else.

## Quick start — Umbraco

Install `NJobDesk.Umbraco` and the dashboard appears under **Settings → Jobs**, guarded by backoffice authentication. Plug in Umbraco's own recurring jobs:

```csharp
builder.Services
    .AddNJobDesk()
    .AddEfHistory(HistoryDatabase.Sqlite($"Data Source={historyDbPath}")); // optional

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .AddNJobDeskUmbracoJobs()   // lists + triggers IRecurringBackgroundJob implementations
    .Build();
```

On Umbraco 17.5+ manual triggers go through the native trigger API and every run (scheduled or manual) is recorded from Umbraco's notifications; on Umbraco 16 triggering and history for manual runs still work through a built-in fallback runner.

## Writing a provider

Implement `ISchedulerProvider` (a descriptor with capability flags + the info/management contracts using provider-local, URL-safe ids) and register it with `AddProvider<T>()`. The dashboard handles id prefixing, aggregation, fault isolation, and hides any action your capabilities don't declare. `NJobDesk.Hangfire` is a compact reference implementation.

## Development

- `dotnet build NJobDesk.slnx` / `dotnet test tests/NJobDesk.Tests` (`-p:BuildClient=false` skips the npm build)
- Client: `cd src/NJobDesk.AspNetCore/Client && npm test`
- Regenerate the API client offline: `dotnet run --project demo/Standalone.DemoSite -- --export-openapi src/NJobDesk.AspNetCore/openapi/openapi.json`, then `npm run generate-client`
- Demos: `demo/Standalone.DemoSite` (three fake providers incl. a degraded one), `demo/Hangfire.DemoSite`, `demo/Demo.Umbraco.V16`, `demo/Demo.Umbraco.V17`

MIT licensed.

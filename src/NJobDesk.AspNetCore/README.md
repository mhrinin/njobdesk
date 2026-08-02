# NJobDesk.AspNetCore

The **NJobDesk** dashboard for any ASP.NET Core app: a management API plus an embedded UI (job list, job detail, run history, per-run logs, cron editing with live validation, 24h trend chart) served at a configurable path - like Hangfire's dashboard, but scheduler-agnostic.

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddNJobDeskApi();      // dashboard API + auth (local requests by default)
// ... register a scheduler provider package here (Quartz via uQuartz, Hangfire, Umbraco jobs)

var app = builder.Build();
app.MapControllers();
app.MapNJobDesk();                      // serves the UI at /njobdesk
app.Run();
```

Authorization defaults to local requests only; configure `NJobDeskDashboardOptions.AuthorizationPolicy`, `AuthorizationFilter`, or `ReadOnly` for production setups.

MIT licensed. https://github.com/mhrinin/njobdesk

# NJobDesk

**A scheduler-agnostic jobs dashboard for .NET** - one management UI for background jobs across Quartz.NET (via uQuartz), Hangfire, Umbraco recurring jobs, and any custom scheduler, hosted either as ASP.NET Core middleware (like Hangfire's dashboard) or inside the Umbraco backoffice.

> Extracted from [uQuartz](https://github.com/mhrinin/uQuartz), whose dashboard was already provider-neutral; uQuartz now consumes NJobDesk as its Quartz provider.

## Packages

| Package | Purpose |
|---|---|
| `NJobDesk.Core` | Provider contracts, models, cron validation, null-object defaults |
| `NJobDesk.AspNetCore` | Management API + embedded dashboard UI for any ASP.NET Core app |
| `NJobDesk.History.EFCore` | Persistent execution history + per-run log capture (planned) |
| `NJobDesk.Umbraco` | Umbraco backoffice integration + native recurring-jobs provider (planned) |
| `NJobDesk.Hangfire` | Hangfire provider (planned) |

## Quick start (standalone)

```csharp
builder.Services.AddNJobDeskApi();
// + a scheduler provider package
app.MapControllers();
app.MapNJobDesk();   // UI at /njobdesk
```

## Development

- `dotnet build` (net8.0 + net10.0) / `dotnet test`
- Client: `cd src/NJobDesk.AspNetCore/Client && npm test`
- Regenerate the API client offline: export the spec with `dotnet run --project demo/Standalone.DemoSite -- --export-openapi src/NJobDesk.AspNetCore/openapi/openapi.json`, then `npm run generate-client`
- Demo: `dotnet run --project demo/Standalone.DemoSite` and browse `/njobdesk`

MIT licensed.

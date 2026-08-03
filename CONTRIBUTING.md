# Contributing to NJobDesk

Thanks for considering a contribution! Issues and pull requests are welcome.

## Project layout

- `src/NJobDesk.Core` — the neutral bottom layer: the scheduler-provider abstraction (descriptor, capabilities, registry, aggregation), contracts, models, cron validation, and the `NJobDeskBuilder` returned by `AddNJobDesk()`.
- `src/NJobDesk.AspNetCore` — the ASP.NET Core dashboard: management API controllers + the embedded Lit/TypeScript SPA (`Client/`); references Core only.
- `src/NJobDesk.History.EFCore` — EF Core execution history: DbContexts + migrations (SQL Server/SQLite), the store/writer, per-run log capture seams, retention cleanup and stale-run reconciliation; `.AddEfHistory()` on the builder.
- `src/NJobDesk.Umbraco` — the Umbraco adapter: backoffice composer + bundle build, and the native `IRecurringBackgroundJob` provider; multitargets Umbraco 16 (net9.0) and 17 (net10.0).
- `src/NJobDesk.Hangfire` — the Hangfire recurring-jobs provider.

## Building

.NET SDK 10 and Node 22+ are required (all TFMs build with the 10 SDK).

```
dotnet build NJobDesk.slnx
```

The client is built automatically via MSBuild targets; skip it with `-p:BuildClient=false`.

## Tests

```
dotnet test tests/NJobDesk.Tests                       # net8.0 + net10.0
cd src/NJobDesk.AspNetCore/Client && npm test          # web-test-runner
```

## Code style

The repo enforces its style at build time (`.editorconfig` + `EnforceCodeStyleInBuild` + `TreatWarningsAsErrors`). Run `dotnet format` before committing. The judgment rules live in `.claude/rules/csharp-code-style.md`.

## API client

The TypeScript client under `src/NJobDesk.AspNetCore/Client/src/core/api` is generated — never edit it by hand:

```
dotnet run --project demo/Standalone.DemoSite -- --export-openapi src/NJobDesk.AspNetCore/openapi/openapi.json
cd src/NJobDesk.AspNetCore/Client && npm run generate-client
```

## EF Core migrations

`NJobDesk.History.EFCore` ships two migration sets — one per database provider. Schema changes require regenerating **both** (from the project directory, PowerShell):

```
$env:TargetFramework='net10.0'
dotnet ef migrations add <Name> --context SqliteNJobDeskHistoryDbContext --output-dir Migrations/Sqlite
dotnet ef migrations add <Name> --context SqlServerNJobDeskHistoryDbContext --output-dir Migrations/SqlServer
```

Migrations apply automatically at startup; parity is guarded by `HasPendingModelChanges` tests. SQL Server objects live in the `njobdesk` schema; SQLite tables are unprefixed.

## Demos

- `demo/Standalone.DemoSite` — three fake providers (full-featured, limited-capability, permanently degraded) with seeded history; also the OpenAPI exporter.
- `demo/Hangfire.DemoSite` — Hangfire with in-memory storage, zero Umbraco.
- `demo/Demo.Umbraco.V16` / `demo/Demo.Umbraco.V17` — unattended-install Umbraco sites (`admin@demo.local` / `DemoPass1234!`) with the native jobs provider.

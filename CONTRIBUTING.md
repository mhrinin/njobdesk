# Contributing

## Project layout

The stack is split into a framework-neutral core plus a thin Umbraco adapter / NuGet packages:

- `src/NJobDesk.Core` — the neutral bottom layer: dashboard services, contracts (history store, entities), options, `NJobDeskDatabaseInfo`, and the `NJobDeskBuilder` returned by `AddNJobDesk()`; depends only on NJobDesk.
- `src/NJobDesk.Persistence` — EF Core persistence: schema provisioning, startup migrations, the NJobDesk job store (`QRTZ_` tables) DbContexts/migrations, and the `.AddPersistentScheduler()` builder extension; references Core.
- `src/NJobDesk.History` — execution history: store implementation, migrations, per-run log capture, and the `.AddHistory()` builder extension; references Core and Persistence.
- `src/NJobDesk.AspNetCore` — the ASP.NET Core dashboard: management API controllers + embedded Lit/TypeScript SPA (`Client/`); references Core only.
- `src/Umbraco.Community.NJobDesk` — the Umbraco adapter package: backoffice dashboard tab and Umbraco integration; references NJobDesk.AspNetCore, NJobDesk.History and NJobDesk.Persistence, composes them from the `UmbracoCommunityNJobDesk` config section, and builds the backoffice bundle into its own `wwwroot`.

## Building

```
dotnet build NJobDesk.slnx
```

The client (Lit/TypeScript) is built automatically via MSBuild targets (`npm ci` + `npm run build`).
Skip it with `-p:BuildClient=false`.

## Running the demo sites

```
dotnet run --project demo/PlainAspNetCore.DemoSite          # standalone dashboard at /quartz
dotnet run --project demo/Umbraco.Community.NJobDesk.DemoSite  # Umbraco backoffice integration
```

Backoffice: https://localhost:44330/umbraco — `admin@example.com` / `SuperSecret123!` (unattended install, SQLite).

## Tests

```
dotnet test tests/NJobDesk.Tests
dotnet test tests/NJobDesk.IntegrationTests   # requires Docker (Testcontainers, SQL Server)
```

## EF Core migrations

Each persistence project ships two migration sets — one per database provider. Schema changes require regenerating **both**:

```
dotnet ef migrations add <Name> --project src/NJobDesk.Persistence --context SqlServerNJobDeskDbContext --output-dir Migrations/SqlServer
dotnet ef migrations add <Name> --project src/NJobDesk.Persistence --context SqliteNJobDeskDbContext --output-dir Migrations/Sqlite

dotnet ef migrations add <Name> --project src/NJobDesk.History --context SqlServerNJobDeskHistoryDbContext --output-dir Migrations/SqlServer
dotnet ef migrations add <Name> --project src/NJobDesk.History --context SqliteNJobDeskHistoryDbContext --output-dir Migrations/Sqlite
```

Migrations are applied automatically at startup by `SchemaMigrationHostedService` (registered before NJobDesk's own hosted service).

SQL Server objects live in the `quartz` schema (`quartz.QRTZ_*`, `quartz.NJobDeskExecutionHistory`);
SQLite has no schemas, so tables are unprefixed (`QRTZ_*`, `NJobDeskExecutionHistory`).
The NJobDesk store table prefix must stay in sync: `[quartz].QRTZ_` on SQL Server, `QRTZ_` on SQLite.

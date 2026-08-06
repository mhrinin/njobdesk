# NJobDesk — Scheduler-Agnostic Jobs Dashboard, Extracted from uQuartz

MIT open-source (.NET) management UI for background jobs, extracted from the already provider-neutral dashboard inside `D:\Projects\Umbraco.Community.Quartz`, published on GitHub + NuGet.org, with providers for Quartz (via uQuartz, which becomes a consumer), Umbraco `IRecurringBackgroundJob`, Hangfire, and later Ideo.Umbraco.DataImport. This repo: `D:\Projects\njobdesk`.

## For Future Agents
As work proceeds: mark checkboxes `- [x]` as items complete; when a phase is done, set its status to `Complete` and write its **Phase Summary** (what was done, key decisions, anything needed to continue with zero context); run the phase's **Verification Plan** and record the result before moving on. When all phases are done, fill in **Final Recap** and **Deployment Plan**. If any phase reveals the plan is wrong, contradictory, or impossible, STOP and surface the conflict — silent rescoping is not allowed; the plan is updated through an explicit revision, not absorbed into a phase.

**Code style is mandatory in every phase:** this repo carries `.editorconfig` + `.claude\rules\csharp-code-style.md` (copied in Phase 1) with `EnforceCodeStyleInBuild=true` + `TreatWarningsAsErrors`; `GenerateDocumentationFile` on src projects; EF migrations exempted via nested `Migrations\.editorconfig` (`generated_code=true`); judgment rules apply (one top-level type per file in src, `nameof`/named constants over magic values, records for DTOs). `dotnet format --verify-no-changes` is part of every phase's verification. The same rules are adopted into the uQuartz repo during Phase 7.

**CAUTION for rename/refactor scripts:** exclude `docs\**` (and any prose) from bulk find/replace — the Phase 1 sweep once clobbered this very file.

## Context

The owner (personal OSS, same GitHub account as uQuartz — `mhrinin`) wants a professionally designed jobs dashboard usable with **any scheduler** (Quartz, Hangfire, Umbraco native jobs, DataImport, others) and **any host** (plain ASP.NET Core middleware à la Hangfire Dashboard, plus Umbraco backoffice integration) — an alternative to JobsJobsJobs and Hangfire's dated dashboard.

Exploration of `D:\Projects\Umbraco.Community.Quartz` (post `feature/standalone-dashboard` merge) found the extraction is **~70% done along the Umbraco axis already**:
- `src\uQuartz.AspNetCore` (dashboard host, 5 controllers/20 endpoints, auth, embedded-SPA hosting) is provider-neutral — controllers depend only on `ISchedulerInfoService`, `ISchedulerManagementService`, `IExecutionHistoryStore`, `CronService`.
- The Lit 3 + TS + Vite client (`Client\src\core\`, ~4,200 lines, plain `@umbraco-ui/uui` — NOT backoffice-bound) already dual-builds: standalone embedded SPA + Umbraco backoffice bundle, with a Vite guard that fails the build if a `@umbraco-cms` import leaks into standalone.
- Quartz-specific code is concentrated in 4 files (~580 lines): `SchedulerInfoService`, `SchedulerManagementService`, `QuartzModelMapper` (partially generic — `MapExecution`/`MapExecutionLog` are generic, split into Core), `CronService`.
- MIT LICENSE, GitHub Actions build + release (NuGet **Trusted Publishing via OIDC**) and dependabot already exist in uQuartz. **Nothing is published** (no tags, private repo, nuget.org 404s) → zero back-compat constraints; renames are free.
- UI quality worth preserving: pure-CSS trend charts, `--uui-*` theming (dark mode free), self-contained localization, uui-based modal/toast services mirroring backoffice contracts, log console with level chips/search/copy, colorblind-safe state tags, `data-mark` E2E hooks, designed empty states, embedded-SPA hosting with runtime config injection.

Decisions: name **NJobDesk** (validated free on NuGet + GitHub); **standalone middleware + Umbraco integration** hosts; MIT on GitHub publishing to **public NuGet.org**; providers v1 = Umbraco native + Quartz (via uQuartz) + Hangfire, DataImport provider later; **built before** DataImport Phases 8–11 (`D:\Projects\ideo.umbraco.import\docs\plans\feature-data-import-extension-plan.md`); DataImport keeps its own planned UI ("two UIs") but will also surface in NJobDesk via its provider.

## Package map & repos

| Package | Repo | TFMs | Contents |
|---|---|---|---|
| `NJobDesk.Core` | njobdesk | net8.0;net10.0 | Contracts (`ISchedulerInfoService`, `ISchedulerManagementService`, `IExecutionHistoryStore`, `ICronService`), DTOs/models, provider descriptor + capabilities, null-object fallbacks, builder |
| `NJobDesk.AspNetCore` | njobdesk | net8.0;net10.0 | Controllers, auth (`NJobDeskAuthorization` policy, loopback fallback, read-only filter), route convention (`njobdesk/api/v{version}` default), `MapNJobDesk`, embedded standalone SPA hosting, the client source (dual Vite builds) |
| `NJobDesk.History.EFCore` | njobdesk | net8.0;net10.0 | Neutralized EF history store (tables `NJobDeskExecutionHistory`/`...Log`), SqlServer+Sqlite migrations, retention cleanup (hosted timer, NOT a Quartz job), stale-run reconciliation, `ILogger` per-run capture seams |
| `NJobDesk.Umbraco` | njobdesk | net9.0;net10.0 (Umb `[16,17)`/`[17,18)`) | Backoffice bundle + Settings-section dashboard registration, backoffice auth wiring, Swagger plumbing lifted from uQuartz's Umbraco project, **and the native `IRecurringBackgroundJob` provider** |
| `NJobDesk.Hangfire` | njobdesk | net8.0;net10.0 | Hangfire provider (storage-API read + trigger) |
| `uQuartz.*` / `Umbraco.Community.Quartz` | Umbraco.Community.Quartz | net10.0 | Becomes the **Quartz provider**: keeps `SchedulerInfoService`→`QuartzSchedulerInfoService` etc., Quartz `CronService`, `uQuartz.History` (JobListener + log capture), `uQuartz.Persistence`; consumes NJobDesk packages; its Umbraco package thins to Quartz wiring |
| `Ideo.Umbraco.DataImport.NJobDesk` | ideo.umbraco.import | later | DataImport provider (post-v1; appended to that repo's plan) |

Dev-time cross-repo dependency: uQuartz consumes NJobDesk via a **local folder feed** (`dotnet pack` artifacts) until NuGet.org has it.

## Key design decisions

- **Extraction, not rewrite.** MOVE list (from exploration): all of `uQuartz.AspNetCore` (incl. `Client\src\core\**`, standalone Vite config, web-test-runner tests), Core's `Contracts/Entities/Store/Models` + the four service interfaces + `NotConfigured*` + builder, workflows/LICENSE/dependabot/Directory.Build.props. STAY in uQuartz: the 4 Quartz service/mapper files, `uQuartz.History` (Quartz listener), `uQuartz.Persistence`, Umbraco adapter's Quartz-specific wiring.
- **Rename sweep** (mechanical, touches nearly every file): namespaces `uQuartz.*`→`NJobDesk.*`; `AdduQuartz*`→`AddNJobDesk*`; `MapuQuartz`→`MapNJobDesk`; policy `"uQuartz"`→`"NJobDesk"`; options types; custom-element prefix `quartz-*`→`njd-*`; localization keys `quartzDashboard_*`→`njobdesk_*`; `window.__QUARTZ_DASHBOARD__`→`window.__NJOBDESK__`; `data-mark="quartz:..."`→`"njobdesk:..."`; default ApiPath `quartz/api/v1`→`njobdesk/api/v1`; history tables + migrations regenerated as `NJobDesk*` (`__NJobDeskHistoryMigrations`).
- **Provider model (the new abstraction layer):** `ISchedulerProvider` descriptor (key, display name, version, `SchedulerCapabilities` flags: `TriggerNow`, `Pause`, `ScheduleEditing`, `Groups`, `Triggers`, `History`, `RunLogs`, `Interrupt`) + provider **registry** + **aggregating decorator** implementing the four contracts over N providers (jobs/executions tagged with `providerKey`; per-provider fault isolation → degraded banner, not a blank dashboard). Single-provider setups bypass aggregation. Capabilities surface on `GET /scheduler` and per-job; the client extends the existing `dashboard-state.ts` ReadOnly pattern (already end-to-end) to hide unsupported actions.
- **Identity & triggers modeling** (the two real design risks): job identity becomes single string id + optional group (Quartz keeps group/name; Hangfire/Umbraco get `"default"`); providers without trigger entities project **one synthetic trigger** from the job's schedule; the cron editor is gated by `ScheduleEditing` (Umbraco native jobs use `Period`, not cron → read-only schedule text).
- **DTO vocabulary softening:** `QuartzVersion`→`ProviderVersion`, `JobStoreType`→`StoreType`; `MisfireInstruction`/`Priority`/`Durable` nullable-and-hidden-when-absent.
- **`ICronService` in Core** with a Cronos-based default; uQuartz keeps its Quartz `CronExpression` implementation. (Pulled forward into Phase 1 — required for a Quartz-free build; `CronExpressionDescriptor` stays for human summaries.)
- **Client OpenAPI generation fix:** replace run-a-live-site `generate-openapi.js` with a checked-in spec (exported offline by the standalone demo's `--export-openapi` mode) so `npm run generate-client` works offline.
- **Umbraco native jobs provider:** discovery via `IEnumerable<IRecurringBackgroundJob>`; history through `IExecutionHistoryStore` from Umbraco's recurring-job notifications (**verify exact notification types/payloads empirically; also check the hinted v17 native trigger API**); manual trigger = resolve job in a DI scope + `RunJobAsync` with own CTS + single-flight (coordinator pattern proven in the DataImport engine); cooperative stop only for runs we started.
- **Hangfire provider v1:** `JobStorage.GetConnection().GetRecurringJobs()` for list/last-run/next-run; `IRecurringJobManager.Trigger`; history = LastExecution/LastJobState (full monitoring-API correlation evaluated in-phase: bounded or off); no pause/schedule-edit.
- **Reuse from `ideo.umbraco.import`** (read its plan's phase summaries — they contain the recipes): multitarget CPM rails, dual-provider EF migration recipe (`EF_MIGRATIONS` gating, migrations-folder editorconfig exemption, local dotnet-ef tool), all Umbraco 16/17 seams (`AddUmbracoDbContext` `#if`, package controllers need `AddApplicationPart` + attributes on concrete controllers, Swagger operationId/schema-id workarounds, Swashbuckle-10 `OpenApiInfo` namespace `#if`), demo-host verification harness (client-credentials Api-kind user needing one restart; **V17 token endpoint requires HTTPS**). uQuartz's own Umbraco Swagger plumbing supersedes ours where they overlap — verify it on Umb16 (uQuartz is 17.3-only today).
- uQuartz's README roadmap item "adopt Quartz.HttpApi" is superseded by the provider abstraction — reconcile the README during Phase 7.

## Phase 1: Extraction bootstrap
Status: Complete
Out of scope for this phase: provider model changes, any behavior changes beyond the rename.

- [x] `git init` `D:\Projects\njobdesk`; copy the MOVE list from `D:\Projects\Umbraco.Community.Quartz`; solution `NJobDesk.slnx`
- [x] Rails: Directory.Build.props/CPM; `.editorconfig` + `.claude\rules\` (copied from uQuartz — identical family); `EnforceCodeStyleInBuild=true`; keep uQuartz's `.github\workflows\{build,release}.yml` + dependabot + MIT LICENSE (adjust names)
- [x] Full rename sweep per the list above (namespaces, DI methods, policy, element prefix `njd-`, localization keys, window global, data-marks, ApiPath)
- [x] Multitarget `net8.0;net10.0` for Core/AspNetCore (fix or `#if` any net10-only BCL usage found by compilation)
- [x] `ICronService` + Cronos-based `CronosCronService` in Core (pulled forward); generic mapper parts split into Core (`ExecutionModelMapper`)
- [x] Standalone demo site: plain ASP.NET Core + in-memory `DemoSchedulerProvider` fake (implements the contracts with seeded jobs/runs/logs) + `--export-openapi` mode
- [x] Checked-in OpenAPI spec + offline `npm run generate-client`; port web-test-runner client tests

### Acceptance criteria
1. `dotnet build` + tests green on net8.0 and net10.0; `npm test` green.
2. Standalone demo serves the fully-functional dashboard with fake data at `/njobdesk`.
3. `dotnet pack` produces NJobDesk.Core/AspNetCore nupkgs.

### Verification Plan
- `dotnet build`; `dotnet test`; `cd Client && npm test`; `dotnet format --verify-no-changes`.
- `dotnet run` demo → browse `/njobdesk`: job list, detail, history, logs, trigger action against the fake.

### Phase Summary
Completed 2026-08-02. ALL acceptance criteria verified: 10 .NET tests green on net8.0+net10.0 (no net10-only BCL usage surfaced — the multitarget downgrade was free); 23 client tests green; `dotnet format --verify-no-changes` clean; NJobDesk.Core + NJobDesk.AspNetCore 0.1.0 nupkgs pack; **browser-verified** dashboard at `/njobdesk` on the standalone demo — Overview (KPI tiles, 24h trend chart, live Running-now table with elapsed ticking, scheduler card incl. renamed Provider version/Job store fields), Jobs (state chips, human cron summaries, relative next-fire, Run now/Pause/Resume live, system-job toggle), trigger POST + list refresh + polling all working; only console noise is a missing `/favicon.ico` 404 and a donor `UUI-INPUT needs a label` a11y warning (both Phase-8 polish items).

Structure and deviations a future agent must know:
- Repo: `NJobDesk.slnx`; projects `src\NJobDesk.Core` (net8+net10; deps CronExpressionDescriptor, Cronos, M.E.DependencyInjection.Abstractions), `src\NJobDesk.AspNetCore` (net8+net10; embeds the SPA; npm build runs from MSBuild via `BuildClient` — pass `-p:BuildClient=false` to skip; NOTE `dotnet format` rejects `-p:` — run it without), `tests\NJobDesk.Tests`, `demo\Standalone.DemoSite` (net10; also the OpenAPI exporter).
- **AddNJobDesk (Core DI)** registers TryAdd defaults: `NotConfiguredScheduler{Info,Management}Service`, `EmptyExecutionHistoryStore`, `CronosCronService`, `TimeProvider.System` — providers must register BEFORE `AddNJobDesk*` (TryAdd semantics; verified by test). The Quartz-probe + `uQuartzOptions`/validator did NOT move (they stay in uQuartz).
- **ICronService** (Core) + `CronosCronService`: accepts 5/6-field (Quartz-style tokens ok), rejects 7-field year expressions with a clear error; `CronDescriptions.Describe` (CronExpressionDescriptor) for summaries; `ExecutionModelMapper` carries the generic mapper halves. `SchedulerStatusModel.QuartzVersion/JobStoreType` → `ProviderVersion/StoreType` (Phase-2 softening pulled forward; client + en.ts aligned).
- **OpenAPI pipeline**: checked-in spec at `src\NJobDesk.AspNetCore\openapi\openapi.json`, exported OFFLINE by `dotnet run --project demo\Standalone.DemoSite -- --export-openapi <path>` (builds the app, resolves ISwaggerProvider, writes V3 JSON, exits — no live host). The demo's SwaggerGen needs three things for a faithful spec (all in `Program.cs` + `Demo\RequireNonNullableSchemaFilter.cs`): `DocInclusionPredicate((_,_) => true)` (controllers use per-area ApiExplorer GroupNames), `SupportNonNullableReferenceTypes` + require-non-nullable schema filter (else the generated TS gets everything optional and the client breaks), and `CustomOperationIds` = camelCased action name (the SDK method names — `triggerJob`, `validateCron` — come from operationIds). `npm run generate-client` reads the checked-in spec.
- Rename-sweep extras beyond the plan list: `quartzIcons`→`njdIcons`, localization root object `quartzDashboard`→`njobdesk`, `<!--%QUARTZ_CONFIG%-->`→`%NJOBDESK_CONFIG%`, tsconfig `types` dropped `@umbraco-cms/backoffice/extension-types` (client is now backoffice-free; package.json has no `@umbraco-cms` dep and no umbraco build scripts — those return in Phase 4). Client custom elements/files use the `njd-` prefix.
- Demo fake: `Demo\DemoSchedulerState` (8 jobs/4 groups, one paused + one error-state, ~150 seeded executions over 48h with logs incl. exceptions, 1 live running); management ops mutate state; manual trigger completes after ~4s; reschedule validates via ICronService and returns the updated `TriggerModel` (the controller returns `RescheduleResult.Trigger` — a fake that omits it renders an empty row).
- Workflows: uQuartz's build/release (NuGet Trusted Publishing OIDC) carried over; integration-test step removed until that suite exists. `.gitignore` covers `assets/` (built SPA) + `artifacts/`.
- uQuartz repo remains UNTOUCHED (Phase 7 refactors it).

## Phase 2: Provider model
Status: Complete
Out of scope for this phase: real providers.

- [x] `ISchedulerProvider` descriptor + `SchedulerCapabilities` flags + registry + aggregating decorator (per-provider try/catch + timeout; `providerKey` tagging; single-provider fast path)
- [x] Identity refactor: single string id + optional group; synthetic-trigger projection for trigger-less providers; route/client (`splitId()`) updates
- [x] DTO softening (`ProviderVersion`, `StoreType`, nullable Quartz-flavored fields)
- [x] Client: capability gating (extend ReadOnly pattern), provider grouping/badges, degraded-provider banner

### Acceptance criteria
1. Two fake providers (one healthy, one throwing) → dashboard shows grouped jobs + degraded banner; nothing blank/crashed.
2. Cron editor hidden for a fake without `ScheduleEditing`; trigger button hidden without `TriggerNow`.

### Tests first
- Aggregator: fan-out isolation (throwing provider → degraded entry), key prefixing/round-trip, single-provider bypass.
- Capability serialization on scheduler + job DTOs; synthetic trigger projection.
- Client tests: capability-driven rendering (extend existing specs).

### Verification Plan
- `dotnet test` + `npm test` green; demo scenario with two fakes as above; `dotnet format --verify-no-changes`.

### Phase Summary
Completed 2026-08-02. Verified: 43 .NET tests green on net8.0+net10.0 (up from 10 — CompositeId, registry, both aggregators incl. FakeTimeProvider-driven timeout test, capability JSON serialization, reworked DI tests), 25 client tests green (job-detail suite now covers capability-hidden actions and preview-without-editing), `dotnet format --verify-no-changes` clean, OpenAPI spec re-exported + client regenerated, and **browser-verified** on the 3-provider demo: degraded warning banner for the flaky provider on every tab, aggregated KPI tiles (10 jobs across providers), provider badges on job rows/running table/history rows (auto-hidden when only one provider), per-provider scheduler cards (Started/Started/Unavailable-with-error) with section-level Pause all/Resume all, Basic-provider rows with NO action buttons and a read-only synthetic Cron trigger showing only next-fire preview, Demo-provider trigger + reschedule round-trips working with URL-encoded composite ids (`jobs/demo%3Adefault.cache-warmup/trigger` → 200), network all-200, console clean except the known donor `UUI-INPUT needs a label` warning (Phase 8).

Architecture as built (deviations from the sketch are intentional):
- **Identity**: provider code deals only in provider-local ids (stable, unique within the provider, URL-path-safe — no `/`; documented on `ISchedulerProvider`). The dashboard layer prefixes them via `CompositeId` (`{key}:{localId}`, split on FIRST `:` so local ids may contain colons). The client is fully opaque — `splitId()` deleted; `job.id`/`trigger.id` go straight into routes; colon encodes cleanly on IIS.
- **Contracts**: `ISchedulerInfoService`/`ISchedulerManagementService` remain the PROVIDER contracts, now id-based. Controllers moved to new aggregate-level `IDashboardInfoService`/`IDashboardManagementService` (Core/Providers) instead of the planned "decorator implements the same four contracts" — required because aggregate status is a different shape: `GET /scheduler` now returns `DashboardStatusModel { readOnly, providers: ProviderStatusModel[] }` (descriptor + capabilities + degraded/error + per-provider `SchedulerStatusModel`).
- **`SchedulerCapabilities`** is a record of bools (not a `[Flags]` enum — cleaner JSON/OpenAPI/generated TS), with `None`/`Full` presets and a `Delete` flag added beyond the planned list (gates delete/unschedule UI). Surfaced on provider status AND per job (`JobSummaryModel.Capabilities`, required — providers must set it). Enforced server-side: aggregator refuses actions without the capability (bool ops → false/404; reschedule → new `RescheduleStatus.NotSupported`).
- **Aggregation**: fan-out reads (status/statistics/jobs/running) run per-provider guarded by try/catch + 10s timeout (`AggregatingDashboardInfoService.ProviderCallTimeout`, CTS built with `TimeProvider` so FakeTimeProvider tests it); a failing provider becomes a degraded status entry, never a failed request. Targeted calls (get/trigger/pause/…) route via registry split — no fan-out. Statistics sum + merge buckets by `HourStartUtc`. Jobs fan-out fetches `skip+take` per provider, stamps (`ProviderModels.Stamp` extensions, public), orders provider→group→name, then pages; total = Σ provider totals. No separate single-provider code path (fan-out over 1 is the fast path; badges hide client-side when 1 provider).
- **DI**: `AddNJobDesk` TryAdds registry + both aggregators (NotConfigured* services DELETED — zero providers = unconfigured UX; `UnsupportedSchedulerManagementService` kept public for read-only providers). Providers register via `builder.AddProvider<TProvider>()` (plain multi-registration, order = registry order; keys validated `^[a-z0-9][a-z0-9-]*$`, duplicates throw). Core now references `Microsoft.Extensions.Logging.Abstractions` (aggregators take `ILogger<T>`) — bare-ServiceCollection tests must register `NullLogger<>`.
- **DTO/entity reshape** (Phase 3 builds on this): `SchedulerStatusModel` slimmed (dropped `SchedulerEnabled`/`ProviderVersion`/`ReadOnly`; `ThreadPoolSize` → `int?`); `SchedulerState.NotConfigured` removed; `JobSummaryModel` `{ Id, ProviderKey, Group? }` + `Durable`/`ConcurrentExecutionDisallowed` → `bool?` (detail rows hidden when null); `TriggerModel` `{ Id, Group?, MisfireInstruction?, Priority? }`; `ExecutionModel` `{ ProviderKey, JobId (composite at API), JobGroup?, TriggerName? }` (TriggerGroup dropped). `JobExecutionHistory` entity: `ProviderKey`/`JobId` required, `JobGroup?`/`TriggerId?`/`TriggerName?` — **history writers must stamp ProviderKey**; `ExecutionHistoryFilter` filters by ProviderKey/JobId/JobName. `ExecutionModelMapper.MapExecution` stays LOCAL; the dashboard layer composes (`.Stamp(entry.ProviderKey)` in ExecutionsController). `GET /executions` takes `provider`/`jobId` (composite accepted, split server-side)/`jobName`.
- **Client**: `dashboard-state.ts` gained a providers store (`setProviders`/`findProvider`/`ProvidersController` with `multiProvider`/`degraded`); the shell fetches status on boot and the overview refreshes the store while polling. New `njd-provider-banner` (degraded list above the tabs) and `njd-provider-tag` (auto-hides single-provider). Action cells take `.capabilities` and render nothing without the relevant flags; job-detail gates header buttons and passes job caps to trigger rows; run-details hides the Logs box when `!runLogs` and shows trigger name only when present; edit-schedule modal takes `{ triggerId, name, … }`.
- Demo now registers three providers: `demo` (Full caps, the Phase-1 state, local ids `{group}.{name}`), `basic` (`SchedulerCapabilities.None`, no groups, synthetic triggers — one Cron via ICronService next-fire, one Simple interval), `flaky` (throws everywhere → permanent degraded banner, deliberate showcase). Shared `IExecutionHistoryStore` still registered before `AddNJobDeskApi`.
- Gotcha: after the client bundle contents change, `dotnet build` can fail CS1566 on a stale embedded-asset glob — delete `src\NJobDesk.AspNetCore\assets\` and rebuild.

## Phase 3: History.EFCore neutralization
Status: Complete
Out of scope for this phase: provider-specific history capture.

- [x] Extract EF history from uQuartz.History/Persistence: `NJobDeskExecutionHistory`/`NJobDeskExecutionLog` tables, `__NJobDeskHistoryMigrations`, SqlServer+Sqlite migrations regenerated; nested `Migrations\.editorconfig`
- [x] Retention cleanup as a plain hosted timer (NOT a Quartz job); stale-`Running` reconciliation hosted service
- [x] Provider-neutral `ILogger` per-run capture seams (usable by any provider)

### Acceptance criteria
1. Store round-trips + paging + retention + reconciliation covered by tests on both providers (SQLite in-memory; migration parity via `HasPendingModelChanges`).
2. Standalone demo persists fake-provider runs across restarts.

### Verification Plan
- `dotnet test` green both TFMs; demo restart → history retained; `dotnet format --verify-no-changes`.

### Phase Summary
Completed 2026-08-02. Verified: 57 (net8.0) / 59 (net10.0) tests green — store round-trip/paging/filters/statistics buckets, writer start/complete incl. cross-provider isolation, retention batching, stale-running reconciliation with node prefix, log capture (attach-by-(provider,fireInstance), truncation limits, overflow warning entry, disabled-by-options, no-capture-outside-scope), cleanup + startup-reconciliation services, and migration parity via `HasPendingModelChanges` (net10-only — the API is EF9+, so the parity test file is `#if NET10_0_OR_GREATER`); `dotnet format --verify-no-changes` clean; `NJobDesk.History.EFCore.0.1.0.nupkg` packs. **Demo persistence verified end-to-end over the API**: fresh boot seeds 149 deterministic runs + 1 running into `demo-history.db`; a triggered run persists (Succeeded, ~4s, exactly the two run log entries); after restart the total is retained (150), the seeder does NOT reseed, and the previously-running row is Failed with the startup-reconciliation message.

What was built (`src\NJobDesk.History.EFCore`, net8.0;net10.0, refs Core + EF Sqlite/SqlServer + Design(private)):
- **Schema**: abstract `NJobDeskHistoryDbContext` (tables `NJobDeskExecutionHistory`/`NJobDeskExecutionLog`, migrations table `__NJobDeskHistoryMigrations`) + `Sqlite…`/`SqlServer…` subclasses (SQL Server schema `njobdesk`) + internal design-time factories. Provider-model columns: ProviderKey(64)/JobId(400)/JobGroup?/TriggerId?(400)/TriggerName?; unique index `(ProviderKey, FireInstanceId)`, query index `(ProviderKey, JobId, StartedUtc)`. Migrations generated with global dotnet-ef 10 — multitarget projects fail `--framework` with MSB4057; use **`$env:TargetFramework='net10.0'` then `dotnet ef migrations add <Name> --context Sqlite|SqlServerNJobDeskHistoryDbContext --output-dir Migrations/Sqlite|SqlServer`** from the project dir. EF10-generated migrations compile fine under EF8.
- **EF versions per TFM** (CPM conditional groups): net8→EF 8.0.29 (LTS), net10→EF 10.0.10. NU1903 for `SQLitePCLRaw.lib.e_sqlite3` is emitted by BOTH current EF trains (even 10.0.10) and direct bundle/lib pins don't lift it — it stays a warning under the existing `WarningsNotAsErrors=NU190x`; don't burn time re-pinning.
- **Registration**: `builder.AddEfHistory(HistoryDatabase.Sqlite|SqlServer(conn))` — provider-specific pooled context factory adapted to `IDbContextFactory<NJobDeskHistoryDbContext>` via `DelegatingDbContextFactory`, `SchemaMigrationHostedService` (migrates at startup), `Replace`s `IExecutionHistoryStore`→`EfExecutionHistoryStore` and `IExecutionHistoryWriter`→`EfExecutionHistoryWriter`, startup `ExecutionHistoryReconciliationService`, `ExecutionHistoryCleanupService` (`BackgroundService` + `PeriodicTimer(options.CleanupInterval, timeProvider)`, default 6h; testable `RunCleanupAsync`), and the capture seam. Options `NJobDeskHistoryOptions` bind from `{SectionName}:History` (RetentionDays 30, CleanupInterval, Clustered, Logs{Enabled/MinimumLevel/MaxEntriesPerRun}).
- **New Core contract (pulled in by necessity)**: `IExecutionHistoryWriter` (StartAsync(entry)/CompleteAsync(providerKey, fireInstanceId, status, error)) with a no-op `NullExecutionHistoryWriter` TryAdd default — providers write run lifecycles through it; the EF package replaces it. `JobExecutionLog` writers/store are separate (below).
- **Provider-neutral log capture** (replaces the donor's Quartz `IJobFactory` decorator): public `IExecutionLogCapture.BeginScope()` → `ExecutionLogCaptureScope` (ambient `AsyncLocal` buffer + `RecordException`); an MEL `ILoggerProvider` (TryAddEnumerable) buffers anything logged on the run's async flow; `IExecutionLogStore.SaveAsync(providerKey, fireInstanceId, scope)` attaches entries (with truncation caps + dropped-count warning). **Contract: dispose the scope when the run ends, THEN save** — otherwise the store's own EF command logging gets captured into the buffer it persists (hit this live in the demo; `SaveAsync` now defensively disposes the scope first, but providers should still follow the pattern). Serilog-sink capture did NOT move (uQuartz-specific; Phase 7 decides its fate).
- **Demo**: `DemoSchedulerState` is jobs-only now; history flows through the real store (`AddEfHistory(Sqlite "Data Source=demo-history.db")`). `DemoHistorySeeder` (IHostedService, registered AFTER `AddEfHistory` — hosted services run in registration order and it needs migration+reconciliation first) seeds on empty store only. Trigger flow exercises the real seams: `IExecutionHistoryWriter.StartAsync` → capture scope + real `ILogger` calls → dispose scope → `CompleteAsync` → `IExecutionLogStore.SaveAsync`. `.gitignore` covers `*.db`.
- Tests use `SqliteHistoryDatabaseFixture` (in-memory SQLite kept alive by an open connection, real migrations applied via `Database.Migrate()`, exposed as `IDbContextFactory<NJobDeskHistoryDbContext>`).

## Phase 4: NJobDesk.Umbraco host
Status: Complete
Out of scope for this phase: the native jobs provider (Phase 5).

- [x] Umbraco package project (net9.0;net10.0, Umb `[16,17)`/`[17,18)`); backoffice bundle build (vite.umbraco config + wrapper move here from uQuartz); Settings-section dashboard manifest; backoffice auth defaults; Swagger plumbing (from uQuartz's Umbraco project — verify against Umb16 with the ideo.umbraco.import workarounds where needed)
- [x] Demo.Umbraco.V16 + Demo.Umbraco.V17 hosts (ideo rails: Development env, unattended install, client-credentials user, V17 HTTPS)

### Acceptance criteria
1. Dashboard renders inside the backoffice on BOTH Umb 16 and 17 demos against the fake provider; auth enforced.

### Verification Plan
- Boot both demos; browse Settings → dashboard; API 401 without token, works with backoffice/client-credentials token; `dotnet format --verify-no-changes`.
- If Umb16 proves costly: STOP and check in with the user about descoping v0.1 to 17-only.

### Phase Summary
Completed 2026-08-03. **Browser-verified on BOTH hosts**: unattended-installed demos boot; backoffice login → Settings → "Jobs" dashboard renders the full app (Overview tiles, Fake Scheduler status card, Jobs tab with 3 jobs, capability gating hiding delete/edit for the limited fake); the Pause action round-trips (state chip flips to Paused) on Umb 16.x AND 17.x; unauthenticated `GET /njobdesk/api/v1/scheduler` → 401 on both. Console clean except the known `UUI-INPUT needs a label` warning. Tests 57/59 green, `dotnet format --verify-no-changes` clean, `NJobDesk.Umbraco.0.1.0.nupkg` packs. Umb16 required no descoping.

What was built:
- **`src\NJobDesk.Umbraco`** (Microsoft.NET.Sdk.Razor, net9.0;net10.0, `StaticWebAssetBasePath=/`): refs `Umbraco.Cms.Api.Common`/`Api.Management`/`Web.Common` via new CPM per-TFM groups (`net9.0` → `[16.0.0,17.0.0)`, `net10.0` → `[17.0.0,18.0.0)`; the old net10 group condition changed from `!= 'net8.0'` to `== 'net10.0'` — remember this when adding TFMs). `NJobDeskApiComposer` (auto-discovered): `AddNJobDeskControllers` with `AuthorizationPolicy = SectionAccessSettings` + OpenIddict validation scheme (backoffice bearer guards API and UI); `IOperationIdHandler`/`ISchemaIdHandler` (schema ids expand generics à la Umbraco's `Paged{T}`); PostConfigure SwaggerGen with own `njobdesk` doc + DocInclusionPredicate wrap. **Umb16↔17 seams**: OpenApi.NET v1 vs v2 → `#if NET10_0_OR_GREATER` picks `using Microsoft.OpenApi` vs `Microsoft.OpenApi.Models` and the security-requirement construction (`OpenApiSecuritySchemeReference` vs `Reference = OpenApiReference`); **Umb16 already registers the "Backoffice User" security definition globally — guard `AddSecurityDefinition` with `SecuritySchemes.ContainsKey` or 16 throws duplicate-key at pipeline build** (17 doesn't register it).
- **Bundle build**: donor MSBuild targets adapted — `BuildUmbracoClientAssets` runs `npm run build:umbraco` into `wwwroot\App_Plugins\NJobDesk`, re-globs Content after build, stamps umbraco-package.json version. Client regained `vite.umbraco.config.ts` (lib entry `src/umbraco/manifests.ts`, `@umbraco-cms` external, uui bundled), `src/umbraco/` (dashboard manifest: Settings section, label "Jobs", pathname `njobdesk`; wrapper `njobdesk-umbraco-dashboard` = UmbLitElement passing `umbHttpClient.getConfig()` + `/njobdesk/api/v1` baseUrl into the shared client), `public/umbraco-package.json` (standalone vite config has `publicDir:false` so it only ships in the umbraco build), tsconfig types + devDep `@umbraco-cms/backoffice` **^17.0.0** (16.x pins an exact older `@hey-api/openapi-ts` peer → install conflict; 17 types compile a 16-safe subset, verified live on 16).
- **CRITICAL auth fix in the spec pipeline**: the offline-exported spec had no security schemes → the generated hey-api SDK carried no per-operation `security` metadata → the client never attached the backoffice token → 401s inside the backoffice. The standalone demo's SwaggerGen now declares a `bearer` http scheme + `BearerSecurityOperationFilter` marking every operation, spec + client regenerated (`sdk.gen.ts` now has `security:` per call). Hosts without an auth callback (standalone loopback) simply omit the header. If the dashboard shows endless loading inside Umbraco, check this first.
- **Demos**: `demo\Demo.Fakes` (net8.0, consumed by both hosts) with `FakeSchedulerProvider` (key `fake`, caps TriggerNow+Pause+Triggers, 3 cron jobs, mutable state, no history); `Demo.Umbraco.V16` (net9) / `V17` (net10) — standard Umbraco boot + `AddSingleton<ISchedulerProvider, FakeSchedulerProvider>()` in Program (registration order vs composers is irrelevant). Rails: SQLite `|DataDirectory|` conn string + unattended install (admin@demo.local / DemoPass1234!), **the `umbraco\Data` folder must exist BEFORE first boot** (SQLite error 14 otherwise — each demo's Program.cs creates it), **`ModelsBuilder: Nothing`** (17 makes InMemoryAuto require the DevelopmentMode package; Nothing works on both), HTTPS launch profiles (44316/44317 — 17 needs HTTPS for its token endpoint). `.gitignore` covers umbraco runtime artifacts + the built App_Plugins bundle.
- Serilog note for Phase 5: Umbraco replaces `ILoggerFactory` with Serilog, so the MEL `ILoggerProvider` capture from Phase 3 will NOT see job logs in Umbraco hosts — the donor's Serilog sink (`ExecutionLogCaptureSink`, still in uQuartz) needs a NJobDesk equivalent when Phase 5 wires history capture.

## Phase 5: Umbraco native jobs provider
Status: Complete
Out of scope for this phase: cron-override registration helper (v1.x candidate).

- [x] Discovery via `IEnumerable<IRecurringBackgroundJob>`; job info from Period/Delay (read-only synthetic trigger, no `ScheduleEditing`)
- [x] History via recurring-job notifications → `IExecutionHistoryStore` (verify notification types/payloads empirically)
- [x] Manual trigger: check v17 native trigger API first; fallback scope+`RunJobAsync` with CTS + single-flight; stop = cancel own runs only *(REVISED — see summary: "stop" is empirically impossible on 16 and unnecessary on 17)*
- [x] Sample recurring jobs in the Umbraco demos

### Acceptance criteria
1. Sample jobs listed with persisted history on both demos; trigger + stop work; capabilities hide cron editing.
   *(Revised during the phase: "stop" dropped — Umbraco 16's `RunJobAsync()` takes no CancellationToken so a fallback run cannot be canceled cooperatively, and on 17.5+ triggered runs execute inside Umbraco's own hosted loop which owns their lifetime. `Interrupt` capability stays false; no interrupt API surface was added.)*

### Verification Plan
- Live on both Umbraco demos per acceptance; unit tests for single-flight/stop semantics; `dotnet format --verify-no-changes`.

### Phase Summary
Completed 2026-08-03. **Live-verified on both demos**: the Jobs tab lists `DemoNewsletterDispatchJob` (Umbraco's ~7 built-in jobs correctly hidden behind the system-jobs toggle — anything whose type FullName starts `Umbraco.` is flagged system); Run now on Umb **17.5** goes through the native trigger → the job's own log lines appear → notifications persist the run ("Succeeded 3.0s" in the run timeline); Run now on Umb **16.5** goes through the fallback runner ("Succeeded 3.5s" persisted by the runner itself); scheduled Umbraco jobs on 17.5 stream into history automatically (15 succeeded within minutes of boot); per-job capability gating hides cron editing/pause/delete (only Run now renders). Tests 57/59 green, format clean, all four packages pack.

Empirical findings that shaped the phase (the version matrix matters):
- `AddRecurringBackgroundJob<TJob>` is literally `AddSingleton<IRecurringBackgroundJob, TJob>()` → **discovery via `IEnumerable<IRecurringBackgroundJob>` works on 16 and 17**; local job id = type FullName.
- **Notifications** (`RecurringBackgroundJob{Executing,Executed,Failed,Canceled,Ignored}Notification`, each carrying `.Job`) exist from **Umbraco 17.4**; the **native trigger** (`IRecurringBackgroundJobTrigger<TJob>` + `ITriggerableRecurringBackgroundJob` marker, registered as an open generic) from **17.5**. Umbraco 16 has NEITHER. → the net10 CPM Umbraco range is floored at `[17.5.0,18.0.0)` (this forced the 17.0-installed demo DB through Umbraco's upgrade wizard — one-time, unattended-friendly).
- **CRITICAL gotcha — Executing fires BEFORE the runtime/server-role/MainDom checks**: skipped ticks then publish `Ignored`. Without handling it, provisional Running rows pile up forever (hit live: TouchServer/InstructionProcess stuck "Running"). Fix: handle Ignored and complete the row as **`ExecutionStatus.Vetoed`** (exact semantic match; the client already renders/filters Vetoed). The fire-instance id travels on the **notification `State`** dictionary (`ObjectNotification` is stateful and Umbraco copies state from Executing to the completion notification via `WithStateFrom`) — no correlation dictionary needed.
- Umbraco 16's `IRecurringBackgroundJob.RunJobAsync()` **has no CancellationToken** → the planned "stop own runs" is unimplementable there; plan revised (above), documented on `UmbracoJobFallbackRunner`.
- **EF Core runtime matrix**: Umbraco 16 ships EF Core **9**, and EF8-compiled code throws MissingMethodException on it (`ExecuteUpdate/ExecuteDelete` moved types in EF9). → `NJobDesk.History.EFCore` now multitargets **net8.0;net9.0;net10.0** with per-TFM EF versions (8.0.29 / 9.0.18 / 10.0.10). Any future EF-using package must include a net9 target for Umb16 hosts.
- Per-run log capture for native jobs is confirmed impossible ambient-ly (the run executes on Umbraco's loop flow, not the notification handler's) — already work-wide out of scope; the Phase-4 Serilog-sink note is therefore moot for this provider and belongs to uQuartz (Phase 7).

What was built (`src\NJobDesk.Umbraco\Providers\`): `UmbracoRecurringJobsProvider` (key `umbraco`, version from `IUmbracoVersion`, caps TriggerNow+History; per-job `TriggerNow` reflects `ITriggerableRecurringBackgroundJob` on 17), `UmbracoJobsInfoService` (interval → "Every N minutes" synthetic Simple trigger; prev/next fire derived from the last history row + Period; store-backed statistics/running filtered to this provider's key), `UmbracoJobsManagementService` (17: constructed-generic `IRecurringBackgroundJobTrigger<>` resolved from DI + `TriggerExecution()` via reflection; 16: `UmbracoJobFallbackRunner` — single-flight per job, writes Start/Complete through `IExecutionHistoryWriter`; everything else delegates to `UnsupportedSchedulerManagementService`), `UmbracoJobHistoryNotificationHandler` (net10-only, all five notifications), and `AddNJobDeskUmbracoJobs(this IUmbracoBuilder)` wiring it per TFM. Demos register the provider + `AddEfHistory` (SQLite under `umbraco\Data`, **absolute path** — relative SQLite paths resolve against the process CWD, not content root, and fail under `dotnet run`) + a 2-minute `DemoNewsletterDispatchJob` (17: `RecurringBackgroundJobBase` + triggerable; 16: raw interface with an empty `PeriodChanged` accessor pair). No provider unit tests: the test project's TFMs (net8/net10) can't reference the net9 Umb16 surface and the trigger/notification paths need a composed Umbraco host — the live walkthrough on both demos is the verification, per the plan's acceptance.

## Phase 6: NJobDesk.Hangfire provider
Status: Complete
Out of scope for this phase: Hangfire schedule editing, pause, deep history correlation (evaluate; bounded or off).

- [x] Provider over `JobStorage` connection/monitoring APIs; trigger via `IRecurringJobManager`
- [x] Plain ASP.NET Core demo with Hangfire (memory or SQLite storage) + sample recurring job
- [x] History depth decision documented (LastExecution-only vs bounded monitoring-API correlation)

### Acceptance criteria
1. Hangfire jobs listed with cron + next/last run; trigger works; dashboard fully usable with zero Umbraco packages installed.

### Verification Plan
- Live demo walkthrough; unit tests with substituted storage; `dotnet format --verify-no-changes`.

### Phase Summary
Completed 2026-08-03. Verified live on `demo\Hangfire.DemoSite` (plain ASP.NET Core net10 + Hangfire.AspNetCore + Hangfire.InMemory, **zero Umbraco packages**): three seeded recurring jobs render with human cron summaries ("Every minute"/"Every 2 minutes"/"Every 5 minutes"), real next/last fire times from Hangfire, capability-gated actions (Run now + delete only — no pause/edit); `POST /jobs/hangfire%3Anewsletter-digest/trigger` enqueued and ran the job (console output observed, detail shows the last execution Succeeded); provider card Started / InMemoryStorage / 20 workers. 5 new unit tests run the provider against a REAL `InMemoryStorage` + `RecurringJobManager` (no substitution needed — listing/summary/next-fire, detail trigger + last execution, trigger enqueue + unknown-id refusal, delete round-trip, statistics). 62 (net8) / 64 (net10) tests green, format clean, `NJobDesk.Hangfire.0.1.0.nupkg` packs.

Design decisions of record:
- **History depth: LastExecution-only.** Each recurring job surfaces its `LastExecution`/`LastJobState` as prev-fire and a single synthesized recent execution on the detail (Succeeded/Failed/Running mapped from the state string); runs are NOT archived into NJobDesk history and `GetRunningAsync` returns [] (correlating Hangfire's processing/succeeded monitoring pages back to recurring-job ids requires per-job parameter fetches — unbounded; revisit post-v1 if wanted). Caps: `TriggerNow + Delete + Triggers` (delete maps to `RemoveIfExists`, gating both job delete and trigger unschedule); `History`/`RunLogs` false so the client hides history affordances for these runs.
- Reads go through `JobStorage.GetConnection().GetRecurringJobs()` and `GetMonitoringApi()` (status: Started when servers exist, workers summed into ThreadPoolSize, StoreType = storage type name; statistics: `Recurring` count + `Processing` as running count, no 24h buckets — Hangfire counters are lifetime). **Gotcha: `GetRecurringJobs(ids)` returns a placeholder DTO with `Removed = true` for unknown ids** — existence checks must filter `!dto.Removed` or every id "exists".
- No DI extension needed: `AddProvider<HangfireSchedulerProvider>()` after `AddHangfire`/`AddHangfireServer` resolves `JobStorage` + `IRecurringJobManager` from Hangfire's own registrations. CPM pins Hangfire 1.8.24 / Hangfire.InMemory 1.0.0 (shared group; provider references only Hangfire.Core).
- CronExpressionDescriptor emits 24h-clock summaries here ("At 03:00") — locale-dependent in assertions.

## Phase 7: uQuartz refactor (in D:\Projects\Umbraco.Community.Quartz)
Status: Complete
Out of scope for this phase: uQuartz's own public release.

- [x] Delete moved code; reference NJobDesk packages from the local folder feed; rename kept services `Quartz*`; split generic mapper parts already moved
- [x] `Umbraco.Community.Quartz` thins to Quartz wiring + consumes `NJobDesk.Umbraco` (its own client build targets removed — bundle ships via NJobDesk.Umbraco)
- [x] Keep `.editorconfig` + `.claude\rules\` there (already present); reconcile README (Quartz.HttpApi roadmap item superseded)

### Acceptance criteria
1. Both uQuartz demo sites fully functional through the extracted packages: all 20 endpoints + UI features (schedule editing, history, logs, pause/resume, trigger).
2. uQuartz unit + integration test suites green.

### Verification Plan
- uQuartz `dotnet build`/`test` + demo walkthroughs; `dotnet format --verify-no-changes` in that repo too.

### Phase Summary
Completed 2026-08-03 on uQuartz branch **`feature/njobdesk`** (commit `fa83d98`; mainline untouched). Verified: uQuartz **73 unit + 48 integration tests green** (incl. the Testcontainers SQL Server suites — Docker was available), format clean in BOTH repos. **Live walkthroughs**: PlainAspNetCore demo (`--urls http://localhost:5095`; its default port hit a Windows excluded-port range) — Quartz provider Started/RAMJobStore, jobs with interval summaries, executions flowing into NJobDesk history with composite ids, per-run logs captured through the new scope seams (job ILogger lines + recorded exception), dashboard UI at `/quartz`; Umbraco demo (floats 17.* → 17.5.3, `UpgradeUnattended` handled the DB upgrade; admin@example.com/SuperSecret123!) — Settings→Jobs shows the uQuartz scheduler card, live running execution, and **schedule editing round-trips** ("0 30 3 * * ?" → "At 03:30" persisted through composite trigger id → Quartz), API 401 unauthenticated.

Structure after the refactor (uQuartz consumes NJobDesk 0.1.0 from the local feed `..\njobdesk\artifacts` via nuget.config + CPM entries; **same-version repack requires purging `D:\Software\.nuget\njobdesk.*` before restore**):
- **uQuartz.AspNetCore: DELETED** (project + Client). `uQuartz.Core` = the Quartz provider package (refs NJobDesk.Core): `QuartzSchedulerProvider` (key `quartz`, full caps minus Interrupt; when no `ISchedulerFactory` is registered its Info throws a descriptive InvalidOperationException → degraded card, Management = Unsupported), `QuartzScheduler{Info,Management}Service` (id-based via **`QuartzJobKeys`**: local id = `Escape(group):Escape(name)` — percent-encoding makes the separator unambiguous for arbitrary group/name), `QuartzCronService : ICronService` (registered via TryAdd BEFORE `AddNJobDesk` in `AdduQuartz` so Quartz-exact semantics win over Cronos), slim `HistoryOptions { Enabled, Logs.Enabled }` (retention/cleanup/log-limits bind from the SAME `uQuartz:History` section into `NJobDeskHistoryOptions` — `AdduQuartz` passes its section name to `AddNJobDesk`; `UseClustering` post-configures `NJobDeskHistoryOptions.Clustered`).
- **uQuartz.History** = Quartz capture wiring over NJobDesk.History.EFCore (context/migrations/store/cleanup/reconciliation/MEL-provider all deleted): `ExecutionHistoryListener` writes through `IExecutionHistoryWriter` (Vetoed = pre-completed entry via StartAsync) and saves logs via `IExecutionLogStore` from the scope the reworked `LogCapturingJob`/`Factory` stashes in the job context; `ExecutionLogCaptureSink` (Serilog) feeds `AmbientExecutionLog` (new public seam added to NJobDesk for exactly this) with a testable static `Map`. **`AddQuartzHistory` must reposition NJobDesk's migration + reconciliation hosted services BEFORE Quartz's hosted service** (registration order ≠ start order guarantee; the donor's `SchemaProvisioningOrderTests` caught this regression — matched by ImplementationType name since the NJobDesk types are internal).
- **Umbraco.Community.Quartz** thinned: `QuartzApiComposer` + wwwroot bundle + client MSBuild targets deleted (NJobDesk.Umbraco's composer + App_Plugins bundle arrive transitively); keeps `QuartzComposer` (AdduQuartz with the `UmbracoCommunityQuartz` section), `AddQuartzScheduler`, `UmbracoQuartzDatabase`, the not-configured warning handler. Umbraco range bumped to `[17.5.0,18.0.0)` (NJobDesk.Umbraco's floor). Dashboard tab label is now "Jobs" (NJobDesk's manifest), not "Quartz".
- **Demos**: plain demo uses `AddNJobDeskApi()` + `AdduQuartz().AddHistory(db)` + `MapNJobDesk("/quartz")` (UI path is host-chosen; the API path stays `njobdesk/api/v1` everywhere — one generated client works across hosts). Umbraco demo Program unchanged.
- **Tests**: deleted the suites duplicating njobdesk coverage (buffer/log-store/cleanup/reconciliation/store stats); adapted the rest to composite ids + `DashboardStatusModel` + provider resolution (`GetRequiredService<ISchedulerProvider>().Info/.Management`); fixture maps `/quartz` for UI, API assertions on `/njobdesk/api/v1`; SqlServer schema assertions split across `quartz` (QRTZ_) and `njobdesk` (history) schemas; the old `DELETE jobs/any/job` (two segments) is now a single composite segment (two-segment paths 405). New `NJobDeskCaptureFactory` builds a real capture scope through DI (scope ctor is internal to NJobDesk); it must register an `IConfiguration` — that surfaced a real NJobDesk bug, fixed in njobdesk `b4f2a91`: **`AddEfHistory` now binds options tolerantly via `GetService<IConfiguration>()` instead of `BindConfiguration`** (bare service collections no longer throw on options resolution).

## Phase 8: OSS release
Status: In progress — automatable prep done 2026-08-03; the remaining items are owner-side (see below)
Out of scope for this phase: uQuartz's own release (owner's separate call); DataImport provider.

- [x] README + docs (quick start per host: plain ASP.NET Core, Umbraco; per provider: native, Hangfire, Quartz-via-uQuartz; screenshots pending), CONTRIBUTING, `umbraco-marketplace.json` for NJobDesk.Umbraco (packed at nupkg root)
- [x] Workflows finalized (build.yml: build + unit + client tests + pack on push/PR; release.yml: tag-driven version, tests, pack, NuGet Trusted Publishing via `NuGet/login@v1` with `vars.NUGET_USER`, GitHub Release)
- [x] Create `github.com/mhrinin/njobdesk` (public) and push — DONE 2026-08-06 via `gh` CLI (branch renamed `master`→`main`, default branch `main`, pushed at `07735ee`)
- [ ] **OWNER-SIDE**: configure NuGet.org Trusted Publishing policies for `NJobDesk.Core|AspNetCore|History.EFCore|Umbraco|Hangfire` (nuget.org → account → Trusted Publishing: repository `mhrinin/njobdesk`, workflow `release.yml`) + set the `NUGET_USER` repository variable on GitHub to the nuget.org username; add screenshots to the README; tag `v0.1.0` + push the tag → release workflow publishes (do NOT tag before Trusted Publishing is configured — the push step would fail); scratch-consumer acceptance from the public feed

### Acceptance criteria
1. Final acceptance walkthrough: (a) plain ASP.NET Core + Hangfire, no Umbraco anywhere; (b) Umb 16 + 17 with native provider; (c) uQuartz demos via local feed; (d) scratch consumers installing the PUBLISHED packages.

### Verification Plan
- The walkthrough above, recorded in Final Recap; release workflow run green.

### Phase Summary
_(write when phase completes)_

## New dependencies or infrastructure
- Cronos (Core's default `ICronService`); everything else already present in the donor repos.
- Public GitHub repo + NuGet.org Trusted Publishing (workflow exists; NuGet-side trust policy must be configured by the owner for the new package ids — manual step, flag at Phase 8).
- Local folder feed for cross-repo dev (`artifacts/` + nuget.config entry in consuming repos).

## Edge cases and risks
- Phase 2 identity/synthetic-trigger refactor ripples through routes, DTOs, and the client — the ported client tests + fake providers are the safety net.
- Umb16 support is new for this codebase (uQuartz is 17.3+) — believed safe from ideo.umbraco.import findings; Phase 4 verifies; descope to 17-only requires explicit user check-in.
- net8.0 downgrade of currently-net10 code may hit newer BCL usages — compile-first discovery, fix or `#if`.
- `InternalsVisibleTo` chains recut across the new package boundary.
- Umbraco notification payloads / v17 trigger API / Hangfire history correlation: verified empirically in their phases.

## Out of scope (work-wide)
- DataImport provider (`Ideo.Umbraco.DataImport.NJobDesk`) and DataImport Phases 8–11 — resume in that repo afterwards.
- Quartz provider improvements beyond parity (stays uQuartz's roadmap).
- Native-job cron-override registration helper; per-run log capture for native Umbraco jobs (v1.x candidates).
- SSE/live-progress transport (current client polls with a user-facing Live toggle; the DataImport provider may motivate SSE later).

## Open questions
- None blocking. NuGet Trusted-Publishing trust policy for the new package ids is an owner-side manual step (Phase 8).

## Final Recap
_(write when all phases complete)_

## Deployment Plan
_(write when all phases complete: tag → Actions release → NuGet.org; marketplace listing; announcement)_

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
Status: In progress
Out of scope for this phase: provider model changes, any behavior changes beyond the rename.

- [ ] `git init` `D:\Projects\njobdesk`; copy the MOVE list from `D:\Projects\Umbraco.Community.Quartz`; solution `NJobDesk.slnx`
- [ ] Rails: Directory.Build.props/CPM; `.editorconfig` + `.claude\rules\` (copied from uQuartz — identical family); `EnforceCodeStyleInBuild=true`; keep uQuartz's `.github\workflows\{build,release}.yml` + dependabot + MIT LICENSE (adjust names)
- [ ] Full rename sweep per the list above (namespaces, DI methods, policy, element prefix `njd-`, localization keys, window global, data-marks, ApiPath)
- [ ] Multitarget `net8.0;net10.0` for Core/AspNetCore (fix or `#if` any net10-only BCL usage found by compilation)
- [ ] `ICronService` + Cronos-based `CronosCronService` in Core (pulled forward); generic mapper parts split into Core (`ExecutionModelMapper`)
- [ ] Standalone demo site: plain ASP.NET Core + in-memory `DemoSchedulerProvider` fake (implements the contracts with seeded jobs/runs/logs) + `--export-openapi` mode
- [ ] Checked-in OpenAPI spec + offline `npm run generate-client`; port web-test-runner client tests

### Acceptance criteria
1. `dotnet build` + tests green on net8.0 and net10.0; `npm test` green.
2. Standalone demo serves the fully-functional dashboard with fake data at `/njobdesk`.
3. `dotnet pack` produces NJobDesk.Core/AspNetCore nupkgs.

### Verification Plan
- `dotnet build`; `dotnet test`; `cd Client && npm test`; `dotnet format --verify-no-changes`.
- `dotnet run` demo → browse `/njobdesk`: job list, detail, history, logs, trigger action against the fake.

### Phase Summary
_(write when phase completes)_

## Phase 2: Provider model
Status: Not started
Out of scope for this phase: real providers.

- [ ] `ISchedulerProvider` descriptor + `SchedulerCapabilities` flags + registry + aggregating decorator (per-provider try/catch + timeout; `providerKey` tagging; single-provider fast path)
- [ ] Identity refactor: single string id + optional group; synthetic-trigger projection for trigger-less providers; route/client (`splitId()`) updates
- [ ] DTO softening (`ProviderVersion`, `StoreType`, nullable Quartz-flavored fields)
- [ ] Client: capability gating (extend ReadOnly pattern), provider grouping/badges, degraded-provider banner

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
_(write when phase completes)_

## Phase 3: History.EFCore neutralization
Status: Not started
Out of scope for this phase: provider-specific history capture.

- [ ] Extract EF history from uQuartz.History/Persistence: `NJobDeskExecutionHistory`/`NJobDeskExecutionLog` tables, `__NJobDeskHistoryMigrations`, SqlServer+Sqlite migrations regenerated; nested `Migrations\.editorconfig`
- [ ] Retention cleanup as a plain hosted timer (NOT a Quartz job); stale-`Running` reconciliation hosted service
- [ ] Provider-neutral `ILogger` per-run capture seams (usable by any provider)

### Acceptance criteria
1. Store round-trips + paging + retention + reconciliation covered by tests on both providers (SQLite in-memory; migration parity via `HasPendingModelChanges`).
2. Standalone demo persists fake-provider runs across restarts.

### Verification Plan
- `dotnet test` green both TFMs; demo restart → history retained; `dotnet format --verify-no-changes`.

### Phase Summary
_(write when phase completes)_

## Phase 4: NJobDesk.Umbraco host
Status: Not started
Out of scope for this phase: the native jobs provider (Phase 5).

- [ ] Umbraco package project (net9.0;net10.0, Umb `[16,17)`/`[17,18)`); backoffice bundle build (vite.umbraco config + wrapper move here from uQuartz); Settings-section dashboard manifest; backoffice auth defaults; Swagger plumbing (from uQuartz's Umbraco project — verify against Umb16 with the ideo.umbraco.import workarounds where needed)
- [ ] Demo.Umbraco.V16 + Demo.Umbraco.V17 hosts (ideo rails: Development env, unattended install, client-credentials user, V17 HTTPS)

### Acceptance criteria
1. Dashboard renders inside the backoffice on BOTH Umb 16 and 17 demos against the fake provider; auth enforced.

### Verification Plan
- Boot both demos; browse Settings → dashboard; API 401 without token, works with backoffice/client-credentials token; `dotnet format --verify-no-changes`.
- If Umb16 proves costly: STOP and check in with the user about descoping v0.1 to 17-only.

### Phase Summary
_(write when phase completes)_

## Phase 5: Umbraco native jobs provider
Status: Not started
Out of scope for this phase: cron-override registration helper (v1.x candidate).

- [ ] Discovery via `IEnumerable<IRecurringBackgroundJob>`; job info from Period/Delay (read-only synthetic trigger, no `ScheduleEditing`)
- [ ] History via recurring-job notifications → `IExecutionHistoryStore` (verify notification types/payloads empirically)
- [ ] Manual trigger: check v17 native trigger API first; fallback scope+`RunJobAsync` with CTS + single-flight; stop = cancel own runs only
- [ ] Sample recurring jobs in the Umbraco demos

### Acceptance criteria
1. Sample jobs listed with persisted history on both demos; trigger + stop work; capabilities hide cron editing.

### Verification Plan
- Live on both Umbraco demos per acceptance; unit tests for single-flight/stop semantics; `dotnet format --verify-no-changes`.

### Phase Summary
_(write when phase completes)_

## Phase 6: NJobDesk.Hangfire provider
Status: Not started
Out of scope for this phase: Hangfire schedule editing, pause, deep history correlation (evaluate; bounded or off).

- [ ] Provider over `JobStorage` connection/monitoring APIs; trigger via `IRecurringJobManager`
- [ ] Plain ASP.NET Core demo with Hangfire (memory or SQLite storage) + sample recurring job
- [ ] History depth decision documented (LastExecution-only vs bounded monitoring-API correlation)

### Acceptance criteria
1. Hangfire jobs listed with cron + next/last run; trigger works; dashboard fully usable with zero Umbraco packages installed.

### Verification Plan
- Live demo walkthrough; unit tests with substituted storage; `dotnet format --verify-no-changes`.

### Phase Summary
_(write when phase completes)_

## Phase 7: uQuartz refactor (in D:\Projects\Umbraco.Community.Quartz)
Status: Not started
Out of scope for this phase: uQuartz's own public release.

- [ ] Delete moved code; reference NJobDesk packages from the local folder feed; rename kept services `Quartz*`; split generic mapper parts already moved
- [ ] `Umbraco.Community.Quartz` thins to Quartz wiring + consumes `NJobDesk.Umbraco` (its own client build targets removed — bundle ships via NJobDesk.Umbraco)
- [ ] Keep `.editorconfig` + `.claude\rules\` there (already present); reconcile README (Quartz.HttpApi roadmap item superseded)

### Acceptance criteria
1. Both uQuartz demo sites fully functional through the extracted packages: all 20 endpoints + UI features (schedule editing, history, logs, pause/resume, trigger).
2. uQuartz unit + integration test suites green.

### Verification Plan
- uQuartz `dotnet build`/`test` + demo walkthroughs; `dotnet format --verify-no-changes` in that repo too.

### Phase Summary
_(write when phase completes)_

## Phase 8: OSS release
Status: Not started
Out of scope for this phase: uQuartz's own release (owner's separate call); DataImport provider.

- [ ] README + docs (quick start per host: plain ASP.NET Core, Umbraco; per provider: native, Hangfire, Quartz-via-uQuartz; screenshots), CONTRIBUTING, `umbraco-marketplace.json` for NJobDesk.Umbraco
- [ ] Workflows finalized (build matrix incl. npm tests; release via NuGet Trusted Publishing OIDC); repo public on the owner's GitHub account
- [ ] Tag `v0.1.0` → packages on NuGet.org; scratch-consumer acceptance from the public feed

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

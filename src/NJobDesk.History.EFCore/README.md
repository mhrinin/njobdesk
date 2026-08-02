# NJobDesk.History.EFCore

EF Core execution-history storage for **NJobDesk** — a scheduler-agnostic jobs dashboard for .NET.

Persists run history (duration, outcome, error, node) and per-run `ILogger` output to SQL Server or SQLite for any NJobDesk scheduler provider. Schema migrations apply at startup; runs left behind by a crashed process are reconciled; finished runs past the retention window are pruned on a timer.

```csharp
builder.Services
    .AddNJobDesk()
    .AddEfHistory(HistoryDatabase.Sqlite("Data Source=njobdesk-history.db"));
```

Options bind from `NJobDesk:History` (retention days, cleanup interval, clustered reconciliation, log-capture level and limits).

Provider packages record runs through `IExecutionHistoryWriter` and capture per-run logs via `IExecutionLogCapture` + `IExecutionLogStore`.

MIT licensed. https://github.com/mhrinin/njobdesk

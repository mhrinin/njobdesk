# NJobDesk.Umbraco

Umbraco backoffice integration for **NJobDesk** — a scheduler-agnostic jobs dashboard for .NET.

Adds a professionally designed jobs dashboard to the Settings section of the Umbraco backoffice (Umbraco 16 and 17), guarded by backoffice authentication. Works with any NJobDesk scheduler provider: Umbraco recurring jobs, Quartz (via uQuartz), or Hangfire — including several at once.

Install the package and it wires itself up through a composer; plug in providers where you configure services:

```csharp
builder.Services
    .AddNJobDesk()
    .AddProvider<MySchedulerProvider>();
```

The dashboard API serves at `/njobdesk/api/v1` behind the backoffice Settings-section policy, with a `njobdesk` Swagger document registered alongside Umbraco's.

MIT licensed. https://github.com/mhrinin/njobdesk

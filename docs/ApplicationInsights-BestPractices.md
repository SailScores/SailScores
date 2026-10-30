# Application Insights Best Practices for SailScores

## Overview

SailScores sends server telemetry to Application Insights using OpenTelemetry and the Azure Monitor distro (`Azure.Monitor.OpenTelemetry.AspNetCore`). The classic Application Insights SDK (`TelemetryClient`, telemetry initializers and processors, `ApplicationInsights:SamplingSettings`) is not used; settings for it have no effect.

Setup lives in `ConfigureAppInsightsTelemetry` in `SailScores.Web/Startup.cs`. Telemetry is only enabled when a connection string is configured.

What is collected:

| Telemetry | App Insights table | Sampled? |
|-----------|--------------------|----------|
| Incoming requests | `requests` | Yes, rate-limited |
| Outgoing SQL and HTTP calls | `dependencies` | Yes, follows the parent request |
| `ILogger` output | `traces` | No, filtered by log level only |
| Logged exceptions | `exceptions` | No, filtered by log level only |
| Standard ASP.NET Core and runtime metrics | `customMetrics` / `performanceCounters` | No |

## Configuration

### Where settings live

`SailScores.Web/appsettings.OpenTelemetry.json` holds the telemetry defaults. `Program.cs` loads it directly after `appsettings.json`, so these sources override it, in increasing precedence:

1. `appsettings.{Environment}.json`
2. User secrets (Development)
3. Environment variables, including Azure App Service application settings
4. Command-line arguments

This means production values can be tuned in **Azure Portal > App Service > Configuration** without a redeploy.

### Settings

| Setting | App Service name | Default | Purpose |
|---------|------------------|---------|---------|
| Connection string | `APPLICATIONINSIGHTS__CONNECTIONSTRING` (or `ApplicationInsights__ConnectionString`) | empty (telemetry off) | Identifies the Application Insights resource |
| `OpenTelemetry:TracesPerSecond` | `OpenTelemetry__TracesPerSecond` | `1.0` | Maximum sampled requests per second |
| `Logging:LogLevel:Default` | `Logging__LogLevel__Default` | `Warning` | Minimum level for exported logs |
| `Logging:LogLevel:<Category>` | `Logging__LogLevel__<Category>` | see file | Per-category overrides |

For local development, leave the connection string empty so nothing is sent.

### Sampling

- **Requests** use the distro's rate-limited sampler (`AzureMonitorOptions.TracesPerSecond`). The sample rate is stamped on each item, so request counts and rates in the portal are scaled back up and stay accurate.
- **Dependencies** follow their parent request's sampling decision, so a sampled request keeps its complete set of SQL and HTTP calls.
- **Errors**: a sampler decides when a request starts, before its status code is known, so failed requests are sampled like any other. To avoid losing errors, `EnableTraceBasedLogsSampler` is turned off: every log at or above the configured level is exported, even if its request was not sampled. Log errors with `ILogger` and they will reach the `traces` and `exceptions` tables.
- **Security-scanner requests** (paths containing `.php`, `.asp`, `.jsp`, `.cgi`, `.git` or `.env`) are not traced at all, so they don't consume the sampling budget. See `SailScores.Web/Services/TelemetryRequestFilter.cs`.

### Log levels and cost

Logs are not sampled, so log levels are the main control on volume.

- Keep `Default` at `Warning` in production.
- Never set `Microsoft.EntityFrameworkCore.Database.Command` to `Information` in production. It logs every SQL statement, several per page view. To debug queries, raise it temporarily through an App Service setting and remove the setting afterward.
- Logging categories apply to all providers. There is no separate `Logging:ApplicationInsights` section; that belonged to the classic SDK.

## Logging Best Practices

### Use ILogger

Use `ILogger<T>` for all logging. Do not inject `TelemetryClient`; it belongs to the classic SDK and is not registered.

```csharp
public class RaceService
{
    private readonly ILogger<RaceService> _logger;

    public RaceService(ILogger<RaceService> logger)
    {
        _logger = logger;
    }

    public async Task SaveAsync(Race race)
    {
        try
        {
            // ...
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save race {RaceId}", race.Id);
            throw;
        }
    }
}
```

### Structured logging

Use named placeholders, not string interpolation. Placeholder values become queryable `customDimensions`.

```csharp
// Good
_logger.LogWarning("Race {RaceId} has {ResultCount} results but no competitors", raceId, resultCount);

// Poor: the values are baked into the message and can't be queried
_logger.LogWarning($"Race {raceId} has {resultCount} results but no competitors");
```

### Log levels

Only `Warning` and above are exported by default, so choose the level by whether the event should reach Application Insights:

- **Critical / Error**: an operation failed. Always exported.
- **Warning**: something unexpected that may need attention. Exported.
- **Information**: normal application flow. Not exported unless the category is raised.
- **Debug / Trace**: local diagnostics only.

## Monitoring Queries

Run these in **Azure Portal > Application Insights > Logs**.

### Ingestion volume by table

```kusto
union withsource=Table *
| where timestamp > ago(1d)
| summarize GB = sum(_BilledSize) / 1e9, Rows = count() by Table
| order by GB desc
```

### Noisiest log categories

```kusto
traces
| where timestamp > ago(1d)
| summarize Rows = count() by Category = tostring(customDimensions.CategoryName), severityLevel
| order by Rows desc
```

### Slow database calls

```kusto
dependencies
| where type == "SQL"
| where duration > 1000  // more than 1 second
| project timestamp, target, name, duration
| order by duration desc
| take 20
```

### Slow requests

```kusto
requests
| where duration > 5000  // more than 5 seconds
| project timestamp, name, url, duration, resultCode
| order by duration desc
```

### Recent exceptions

```kusto
exceptions
| order by timestamp desc
| take 50
| project timestamp, type, outerMessage, operation_Name
```

### Failed requests by operation

Request telemetry is sampled, so these counts cover sampled requests only (`itemCount` scales them back up).

```kusto
requests
| where success == false
| summarize Failures = sum(itemCount) by operation_Name, resultCode
| order by Failures desc
```

## Troubleshooting

### No telemetry appearing

1. Check that `APPLICATIONINSIGHTS__CONNECTIONSTRING` (or `ApplicationInsights__ConnectionString`) is set in App Service configuration and is not truncated.
2. Restart the App Service after changing it.
3. Check **App Service > Log stream** for exporter errors at startup.

### High ingestion costs

1. Run the **Ingestion volume by table** query above.
2. **`traces` or `exceptions` largest**: run **Noisiest log categories** and raise the level for the offending category.
3. **`requests` or `dependencies` largest**: lower `OpenTelemetry__TracesPerSecond` (for example to `0.5`).
4. Set a **daily cap** in **Application Insights > Usage and estimated costs** as a backstop.

### Setting changes have no effect

- Make sure the setting is one listed under [Settings](#settings). Classic SDK settings such as `ApplicationInsights:EnableAdaptiveSampling` or `SamplingSettings` are ignored.
- App Service setting names use `__` in place of `:`.

## Client-side (browser) telemetry

Users can opt in to browser analytics with the **Enable Browser Analytics** profile setting (`EnableAppInsights`). For those users, both layouts render the script from `OpenTelemetryJavaScriptService`. That script currently has no exporter configured, so no browser telemetry reaches Application Insights. Replacing it with the Application Insights JavaScript SDK is planned.

## Alerts

Suggested alert rules (**Application Insights > Alerts > New alert rule**):

- **Exceptions**: exception count above a threshold over 5 minutes.
- **Failed requests**: failed request rate above 5%.
- **Response time**: average server response time above 5 seconds.

Because logs are exported unsampled, alerts based on `exceptions` or Error-level `traces` see every occurrence. Alerts based on `requests` should use `itemCount` or the standard metrics, which account for sampling.

## See Also

- [Azure Monitor OpenTelemetry for .NET](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-enable?tabs=aspnetcore)
- [Sampling in Azure Monitor OpenTelemetry](https://learn.microsoft.com/azure/azure-monitor/app/opentelemetry-sampling)
- [Kusto Query Language](https://learn.microsoft.com/kusto/query/)

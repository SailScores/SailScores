# Application Insights Best Practices for SailScores

## Overview

This document provides detailed guidelines for using Application Insights in the SailScores application running on Azure App Service (Linux).

## Table of Contents

1. [Logging Best Practices](#logging-best-practices)
2. [Configuration Guidelines](#configuration-guidelines)
3. [Monitoring Queries](#monitoring-queries)
4. [Troubleshooting Guide](#troubleshooting-guide)
5. [Performance Considerations](#performance-considerations)
6. [Alerts and Notifications](#alerts-and-notifications)

## Logging Best Practices

### Use ILogger, Not TelemetryClient

Always use `ILogger<T>` for all logging. The Application Insights SDK automatically captures logs through the logging infrastructure.

**✅ Correct:**
```csharp
public class RaceService
{
    private readonly ILogger<RaceService> _logger;
    private readonly IRaceRepository _raceRepository;
    
    public RaceService(ILogger<RaceService> logger, IRaceRepository raceRepository)
    {
        _logger = logger;
        _raceRepository = raceRepository;
    }
    
    public async Task<Race> CreateRaceAsync(CreateRaceRequest request)
    {
        _logger.LogInformation("Creating race: {RaceName} for series {SeriesId}", 
            request.Name, request.SeriesId);
        
        try
        {
            var race = new Race { /* ... */ };
            await _raceRepository.SaveAsync(race);
            
            _logger.LogInformation("Race created successfully: {RaceId}", race.Id);
            return race;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create race: {RaceName}", request.Name);
            throw;
        }
    }
}
```

**❌ Incorrect:**
```csharp
// Don't inject TelemetryClient directly
public class RaceService
{
    private readonly TelemetryClient _telemetryClient;
    
    public RaceService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }
    
    public void ProcessRace()
    {
        // This is not recommended - use ILogger instead
        _telemetryClient.TrackTrace("Processing race", SeverityLevel.Information);
    }
}
```

### Structured Logging

Always use structured logging with named parameters. This makes logs queryable and provides better insights.

**✅ Good:**
```csharp
_logger.LogInformation(
    "Race {RaceId} completed. Competitors: {CompetitorCount}, Results: {ResultCount}",
    raceId, competitorCount, resultCount);
```

**❌ Poor:**
```csharp
_logger.LogInformation($"Race {raceId} completed. Competitors: {competitorCount}, Results: {resultCount}");
```

### Log Levels

Use appropriate log levels:

- **Critical**: The service cannot continue (rare)
- **Error**: An operation failed, but the service continues
- **Warning**: Something unexpected happened, but it might be recoverable
- **Information**: High-level application flow events
- **Debug**: Detailed diagnostic information (usually disabled in production)
- **Trace**: Very detailed diagnostic information (usually disabled)

**Example:**
```csharp
public class ScoringSvc
{
    private readonly ILogger<ScoringService> _logger;
    
    public void CalculateScores()
    {
        _logger.LogInformation("Starting score calculation");
        
        try
        {
            // Processing...
            if (unusualCondition)
            {
                _logger.LogWarning("Unusual scoring scenario detected: {Scenario}", scenario);
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Score calculation failed");
            throw;
        }
    }
}
```

## Configuration Guidelines

### Azure App Service Environment Variables

Set these in **Azure Portal > App Service > Configuration > Application Settings**:

```
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=<key>;IngestionEndpoint=https://<region>.in.applicationinsights.azure.com/;LiveEndpoint=https://<region>.livediagnostics.monitor.azure.com/
Logging__LogLevel__Default=Information
Logging__ApplicationInsights__LogLevel__Default=Information
Logging__LogLevel__Microsoft.EntityFrameworkCore=Information
```

### Local Development Configuration

For local development, you can disable Application Insights to avoid costs:

In `appsettings.Development.json`:
```json
{
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    },
    "Console": {
      "IncludeScopes": true
    }
  }
}
```

### Sampling Configuration

The sampling settings in `appsettings.ApplicationInsights.json` control telemetry volume:

- **MaxTelemetryItemsPerSecond**: 5 items/second (start point, adjust based on traffic)
- **InitialSamplingPercentage**: 100% (sample all initially)
- **MovingAverageRatio**: 0.25 (weight recent data more heavily)
- **ExcludedTypes**: Request (prevent bloat from successful requests)

**Adjusting Sampling:**
- If ingestion costs are too high: Reduce `MaxTelemetryItemsPerSecond` to 2-3
- If you're missing important telemetry: Increase to 10-20
- Exceptions and errors are always tracked (not sampled)

## Monitoring Queries

Access queries in **Azure Portal > Application Insights > Logs**

### Find Most Active Clubs

```kusto
requests
| where customDimensions.ClubInitials != ""
| summarize RequestCount = count() by ClubInitials = tostring(customDimensions.ClubInitials)
| order by RequestCount desc
| take 10
```

### Identify Slow Database Queries

```kusto
dependencies
| where type == "SQL"
| where duration > 1000  // more than 1 second
| project timestamp, target, name, duration
| order by duration desc
| take 20
```

### Slow HTTP Requests

```kusto
requests
| where duration > 5000  // more than 5 seconds
| project timestamp, name, url, duration, resultCode
| order by duration desc
```

### View Recent Exceptions

```kusto
exceptions
| order by timestamp desc
| take 50
| project timestamp, type, message, method
```

### Errors by Operation

```kusto
requests
| where success == false
| summarize ErrorCount = count() by operation_Name, resultCode
| order by ErrorCount desc
```

### User Activity Timeline

```kusto
requests
| where customDimensions.UserId != ""
| project timestamp, name, url, duration
| order by timestamp desc
```

### Performance Degradation Detection

```kusto
requests
| summarize RequestCount = count(), AvgDuration = avg(duration), P95Duration = percentile(duration, 95)
  by bin(timestamp, 5m), name
| order by timestamp desc
| project-reorder timestamp, name, RequestCount, AvgDuration, P95Duration
```

## Troubleshooting Guide

### No Telemetry Appearing

1. **Check Connection String**
   - Verify `APPLICATIONINSIGHTS_CONNECTION_STRING` is set in Azure App Service
   - Ensure it's valid and not truncated
   - Restart the App Service after adding it

2. **Verify Application Insights Resource Exists**
   - Check Azure Portal > Resource Groups > your-group > Application Insights
   - Ensure it's in the same region as your App Service

3. **Check Application Logs**
   - App Service > Log Stream
   - Look for any initialization errors with "Application Insights"

4. **Test with Console**
   ```powershell
   # SSH into the App Service and check logs
   # The application should log something like:
   # "Application Insights initialized"
   ```

### High Ingestion Costs

1. **Enable Adaptive Sampling**
   - Verify `EnableAdaptiveSampling = true` in `Startup.cs`
   - Check `appsettings.ApplicationInsights.json` settings

2. **Reduce MaxTelemetryItemsPerSecond**
   ```json
   "MaxTelemetryItemsPerSecond": 3  // from 5
   ```

3. **Filter Static Resources**
   - The `SailScoresTelemetryProcessor` already filters successful static files
   - Verify it's registered in `Startup.cs`

4. **Review Excluded Types**
   - Check `appsettings.ApplicationInsights.json`
   - Exclude verbose sources like detailed entity framework logs

### Missing Custom Properties (ClubInitials, User)

1. **Verify Route Parameters**
   - Club context comes from `{clubInitials}` route parameter
   - Check if route includes this parameter

2. **Check TelemetryInitializer**
   - Verify `SailScoresTelemetryInitializer` is registered in `Startup.cs`
   - Should include line: `services.AddSingleton<ITelemetryInitializer, SailScoresTelemetryInitializer>();`

3. **Verify User Authentication**
   - User context is added when `HttpContext.User.Identity.IsAuthenticated == true`
   - Check that authentication middleware is configured correctly

### Application Insights Not Tracking Dependency Calls

1. **Verify Dependencies Module is Enabled**
   ```csharp
   options.EnableDependencyTrackingTelemetryModule = true;
   ```

2. **Check for Entity Framework Instrumentation**
   - EF Core calls should be tracked automatically
   - Verify with queries using `dependencies | where type == "SQL"`

3. **HTTP Dependencies**
   - Only external HTTP calls are tracked
   - Internal calls to the same app may not show up

## Performance Considerations

### Memory Impact

- Application Insights SDK: ~30-50MB
- Buffering before sending: ~20MB
- Total overhead: Minimal (<1% of app memory)

### CPU Impact

- Telemetry capture: <0.5% CPU overhead
- Asynchronous transmission: Non-blocking
- No impact on request processing

### Network Impact

- Telemetry is compressed and batched
- Typical overhead: <1-5% additional network
- Data is sent asynchronously (doesn't block requests)

### Optimization Tips

1. **Use Debug log level sparingly**
   - Set to `Information` in production
   - Use `Debug` only when troubleshooting

2. **Filter verbose sources**
   ```json
   "Logging": {
     "LogLevel": {
       "Microsoft.AspNetCore.Mvc": "Warning",
       "Microsoft.EntityFrameworkCore.Database.Command": "Information"
     }
   }
   ```

3. **Monitor ingestion rates**
   - Application Insights > Usage and estimated costs
   - Adjust sampling if costs exceed budget

## Alerts and Notifications

### Setting Up Alerts

1. **Critical Exceptions**
   - Application Insights > Alerts > New alert rule
   - Condition: Exception count > 5 in 5 minutes
   - Action: Email to admin

2. **Performance Degradation**
   - Condition: Average response time > 5 seconds
   - Action: Create incident and notify team

3. **High Error Rate**
   - Condition: Failed requests > 10% of total
   - Action: Page on-call engineer

### Creating Smart Alerts

Use Kusto queries for custom alerts:

```kusto
// Alert if club with high activity is experiencing errors
requests
| where customDimensions.ClubInitials != ""
| summarize TotalRequests = count(), FailedRequests = sum(toint(success == false))
  by ClubInitials = tostring(customDimensions.ClubInitials)
| where FailedRequests > 0
| project ClubInitials, FailureRate = todouble(FailedRequests) / TotalRequests
| where FailureRate > 0.05  // More than 5% failure rate
```

## See Also

- [Azure Monitor Documentation](https://docs.microsoft.com/azure/azure-monitor/)
- [Application Insights for .NET](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Kusto Query Language](https://docs.microsoft.com/azure/data-explorer/kusto/query/)
- [SailScores Documentation](../Development.md)

## Document Version

- Created: January 2025
- Updated: January 2025
- Framework: .NET 10
- Application Insights SDK: 2.23.0
- Target: Azure App Service (Linux)

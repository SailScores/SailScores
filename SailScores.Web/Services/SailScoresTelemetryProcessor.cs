using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace SailScores.Web.Services;

/// <summary>
/// Custom telemetry processor that implements intelligent filtering and sampling
/// to optimize Application Insights costs while maintaining visibility into critical events
/// </summary>
public class SailScoresTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;

    public SailScoresTelemetryProcessor(ITelemetryProcessor next)
    {
        _next = next;
    }

    public void Process(ITelemetry item)
    {
        // Always track exceptions - these are critical
        if (item is ExceptionTelemetry)
        {
            _next.Process(item);
            return;
        }

        // Always track failed requests (status code >= 400)
        if (item is RequestTelemetry request)
        {
            if (request.ResponseCode != null && 
                int.TryParse(request.ResponseCode, out int statusCode) && 
                statusCode >= 400)
            {
                _next.Process(item);
                return;
            }

            // Filter out successful static resource requests to reduce volume
            if (item.Context.GlobalProperties.ContainsKey("IsStaticResource") &&
                request.Success == true)
            {
                // Skip this telemetry item
                return;
            }
        }

        // Could filter out successful SQL dependency calls to reduce volume
        // You can still see failed calls and get overall metrics without every success
        if (item is DependencyTelemetry dependency)
        {
            // Always track failed dependencies
            if (dependency.Success == false)
            {
                _next.Process(item);
                return;
            }

            // Reduce telemetry for successful SQL calls (they're usually high volume)
            // The sampling will handle general reduction, but we can filter specific patterns
            if (dependency.Type == "SQL" && dependency.Success == true)
            {
                // Let adaptive sampling handle SQL - don't filter here unless you want more aggressive reduction
                // return; // Uncomment to filter out ALL successful SQL calls
            }
        }

        // Process all other telemetry
        _next.Process(item);
    }
}

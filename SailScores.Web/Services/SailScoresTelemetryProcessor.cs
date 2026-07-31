using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace SailScores.Web.Services;

/// <summary>
/// Custom telemetry processor that implements intelligent filtering and sampling
/// to optimize Application Insights costs while maintaining visibility into critical events
/// </summary>
public class SailScoresTelemetryProcessor
{
    public void Process(ITelemetry item)
    {
        // Keep the processor in place for future telemetry filtering work, but the current
        // Application Insights package version no longer exposes the old processor chain API
        // used by this project. The method remains available for future use.
        if (item is ExceptionTelemetry)
        {
            return;
        }

        if (item is RequestTelemetry request)
        {
            if (request.ResponseCode != null &&
                int.TryParse(request.ResponseCode, out int statusCode) &&
                statusCode >= 400)
            {
                return;
            }

            if (item.Context.GlobalProperties.ContainsKey("IsStaticResource") &&
                request.Success == true)
            {
                return;
            }
        }
    }
}

using Microsoft.Extensions.Configuration;

namespace SailScores.Web.Services;

/// <summary>
/// Service that generates OpenTelemetry JavaScript SDK initialization code for client-side telemetry.
/// Allows users to opt-in to client-side browser telemetry collection.
/// </summary>
public class OpenTelemetryJavaScriptService
{
    private readonly IConfiguration _configuration;

    public OpenTelemetryJavaScriptService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Generates the OpenTelemetry JavaScript SDK initialization script.
    /// Returns empty string if connection string is not configured.
    /// </summary>
    public string GetInitializationScript()
    {
        var connectionString = _configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrEmpty(connectionString))
        {
            return string.Empty;
        }

        // Extract instrumentation key from connection string
        // Format: InstrumentationKey=<key>;IngestionEndpoint=<endpoint>;...
        var instrKeyPart = connectionString.Split(';')
            .FirstOrDefault(x => x.StartsWith("InstrumentationKey="));
        if (string.IsNullOrEmpty(instrKeyPart))
        {
            return string.Empty;
        }

        var instrKey = instrKeyPart.Replace("InstrumentationKey=", "");

        // Return the OpenTelemetry JavaScript SDK initialization script
        return $@"
<script src=""https://cdn.jsdelivr.net/npm/@opentelemetry/auto@latest/dist/index.global.js""></script>
<script>
(function() {{
    // Initialize OpenTelemetry for browser telemetry
    const {{ trace, context, metrics }} = window.otel;

    if (!trace) return;

    // Configure Azure Monitor exporter via OpenTelemetry
    const tracingProvider = trace.getTracer('SailScores');

    // Capture unhandled errors
    window.addEventListener('error', function(event) {{
        if (event.error) {{
            tracingProvider.getCurrentSpan()?.recordException(event.error);
        }}
    }});

    // Capture unhandled promise rejections
    window.addEventListener('unhandledrejection', function(event) {{
        if (event.reason) {{
            const error = event.reason instanceof Error ? event.reason : new Error(String(event.reason));
            tracingProvider.getCurrentSpan()?.recordException(error);
        }}
    }});
}})();
</script>";
    }
}

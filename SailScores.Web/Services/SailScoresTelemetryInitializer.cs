using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace SailScores.Web.Services;

/// <summary>
/// Custom telemetry initializer for Application Insights that adds SailScores-specific context
/// and filters unnecessary telemetry for cost optimization
/// </summary>
public class SailScoresTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SailScoresTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        // Set cloud role name for better identification in Application Insights
        if (string.IsNullOrEmpty(telemetry.Context.Cloud.RoleName))
        {
            telemetry.Context.Cloud.RoleName = "SailScores.Web";
        }

        // Add custom properties for better filtering and analysis
        var context = _httpContextAccessor.HttpContext;
        if (context != null)
        {
            // Add club context if available from route data
            if (context.GetRouteData()?.Values?.ContainsKey("clubInitials") == true)
            {
                var clubInitials = context.GetRouteData().Values["clubInitials"]?.ToString();
                if (!string.IsNullOrWhiteSpace(clubInitials))
                {
                    telemetry.Context.GlobalProperties["ClubInitials"] = clubInitials;
                }
            }

            // Add user context if authenticated
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                telemetry.Context.User.AuthenticatedUserId = context.User.Identity.Name;
            }
        }

        // Filter out health check requests to reduce noise (if health checks are added)
        if (telemetry is RequestTelemetry requestTelemetry)
        {
            if (requestTelemetry.Url?.AbsolutePath?.Contains("/health", StringComparison.OrdinalIgnoreCase) == true)
            {
                // Reduce sampling for health checks
                requestTelemetry.Context.GlobalProperties["SamplingOverride"] = "Reduce";
            }

            // Filter out static file requests that don't need tracking
            if (IsStaticFileRequest(requestTelemetry.Url))
            {
                // Mark for potential exclusion or reduced sampling
                requestTelemetry.Context.GlobalProperties["IsStaticResource"] = "true";
            }
        }

        // Filter out dependency calls to reduce volume (optional - adjust based on needs)
        if (telemetry is DependencyTelemetry dependencyTelemetry)
        {
            // You can add filters here for specific dependencies if needed
            // For example, exclude certain database queries or external calls
        }
    }

    private static bool IsStaticFileRequest(Uri url)
    {
        if (url == null) return false;

        var path = url.AbsolutePath.ToLowerInvariant();
        return path.EndsWith(".css") ||
               path.EndsWith(".js") ||
               path.EndsWith(".jpg") ||
               path.EndsWith(".jpeg") ||
               path.EndsWith(".png") ||
               path.EndsWith(".gif") ||
               path.EndsWith(".svg") ||
               path.EndsWith(".ico") ||
               path.EndsWith(".woff") ||
               path.EndsWith(".woff2") ||
               path.EndsWith(".ttf") ||
               path.EndsWith(".eot");
    }
}

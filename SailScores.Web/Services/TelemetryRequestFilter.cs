using Microsoft.AspNetCore.Http;

namespace SailScores.Web.Services;

/// <summary>
/// Decides which incoming requests get an OpenTelemetry trace. Requests from security
/// scanners probing for other platforms' files are noise, so they are not traced.
/// </summary>
public static class TelemetryRequestFilter
{
    private static readonly string[] BlockedPathFragments =
    [
        ".php",
        ".asp",
        ".jsp",
        ".cgi",
        ".git",
        ".env"
    ];

    /// <summary>
    /// Returns false for requests that should not be traced.
    /// </summary>
    public static bool ShouldTrace(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path))
        {
            return true;
        }

        return !BlockedPathFragments.Any(fragment =>
            path.Contains(fragment, StringComparison.OrdinalIgnoreCase));
    }
}

using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SailScores.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;
    private readonly TelemetryClient _telemetryClient;

    public ErrorController(
        ILogger<ErrorController> logger,
        TelemetryClient telemetryClient)
    {
        _logger = logger;
        _telemetryClient = telemetryClient;
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int code)
    {
        // handle different codes or just return the default error view
        if (code == 404)
        {
            return View("Error404");
        }

        var exceptionHandlerPathFeature = HttpContext?.Features?.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature != null && _telemetryClient != null)
        {
            _telemetryClient.TrackException(exceptionHandlerPathFeature.Error);
            _telemetryClient.TrackEvent("Error.ServerError", new Dictionary<string, string>
            {
                ["originalPath"] = exceptionHandlerPathFeature?.Path,
                ["error"] = exceptionHandlerPathFeature?.Error?.Message
            });
        }

        return View();
    }
}
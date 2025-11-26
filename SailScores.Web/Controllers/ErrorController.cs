using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace SailScores.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class ErrorController : Controller
{
    private readonly ILogger<ErrorController> _logger;

    public ErrorController(ILogger<ErrorController> logger)
    {
        _logger = logger;
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int code)
    {
        if (code == 404)
        {
            var statusCodeReExecuteFeature = HttpContext?.Features?.Get<IStatusCodeReExecuteFeature>();
            var originalPath = statusCodeReExecuteFeature?.OriginalPath ?? HttpContext?.Request?.Path;
            var originalQueryString = statusCodeReExecuteFeature?.OriginalQueryString;

            _logger.LogWarning("404 Not Found: {Path}{QueryString}", originalPath, originalQueryString);
            return View("Error404");
        }

        var exceptionHandlerPathFeature = HttpContext?.Features?.Get<IExceptionHandlerPathFeature>();
        if (exceptionHandlerPathFeature?.Error != null)
        {
            _logger.LogError(
                exceptionHandlerPathFeature.Error,
                "Unhandled exception occurred on path: {Path}",
                exceptionHandlerPathFeature.Path);
        }

        return View();
    }
}
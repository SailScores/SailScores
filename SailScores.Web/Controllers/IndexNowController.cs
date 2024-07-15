using Microsoft.AspNetCore.Mvc;
using NuGet.Configuration;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class IndexNowController : Controller
{
    private const string webPathDelimiter = "/";
    private readonly AppSettingsService _settings;

    public IndexNowController(
        AppSettingsService settings)
    {
        _settings = settings;
    }

    [Route("IndexNowKey.txt")]
    public async Task<ActionResult> KeyFileAsync()
    {
        var config = _settings.GetIndexNowConfig(base.HttpContext?.Request);
        return Content(config.Token);
    }
}
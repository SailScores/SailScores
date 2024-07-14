using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class IndexNowController : Controller
{
    private const string webPathDelimiter = "/";
    private readonly IndexNow.Configuration _config;

    public IndexNowController(
        AppSettingsService setttngs)
    {
        _config = setttngs.GetIndexNowConfig(this.HttpContext.Request);
    }

    [Route("IndexNowKey.txt")]
    public async Task<ActionResult> KeyFileAsync()
    {
        return Content(_config.Token);
    }
}
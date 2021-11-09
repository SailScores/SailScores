using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Services;
using SailScores.Web.Models.Sitemap;
using Microsoft.Extensions.Configuration;

namespace SailScores.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class SitemapController : Controller
{
    private const string webPathDelimiter = "/";
    private readonly Core.Services.IClubService _clubservice;
    private readonly IConfiguration _config;

    public SitemapController(
        Core.Services.IClubService clubService,
        IConfiguration config)
    {
        _clubservice = clubService;
        _config = config;
    }

    [Route("sitemap.xml")]
    public async Task<ActionResult> SitemapAsync()
    {

        var preferredhost = _config["PreferredHost"];
        // CDN rewrites headers, so needed to make this less dynamic.
        string baseUrl = $"https://{preferredhost}";

        // get a list of public clubs
        var clubs = await _clubservice.GetClubs(false);

        var siteMapBuilder = new SitemapBuilder();
        siteMapBuilder.AddUrl(baseUrl + webPathDelimiter, changeFrequency: ChangeFrequency.Monthly, priority: 0.8);
        siteMapBuilder.AddUrl(baseUrl + Url.Action("News", "Home"), changeFrequency: ChangeFrequency.Monthly, priority: 0.5);
        siteMapBuilder.AddUrl(baseUrl + Url.Action("About", "Home"), changeFrequency: ChangeFrequency.Monthly, priority: 0.5);

        foreach (var club in clubs)
        {
            siteMapBuilder.AddUrl(baseUrl + webPathDelimiter + club.Initials, priority: 1.0);
        }

        // generate the sitemap xml
        string xml = siteMapBuilder.ToString();
        return Content(xml, "text/xml");
    }
}
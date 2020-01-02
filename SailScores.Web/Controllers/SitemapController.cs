using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Core.Services;
using SailScores.Web.Models;
using SailScores.Web.Services;
using SailScores.Web.Models.Sitemap;

namespace SailScores.Web.Controllers
{
    [ApiExplorerSettings(IgnoreApi = true)]
    public class SitemapController : Controller
    {

        private readonly Core.Services.IClubService _clubservice;

        public SitemapController(
            Core.Services.IClubService clubService)
        {
            _clubservice = clubService;
        }

        [Route("sitemap.xml")]
        public async Task<ActionResult> SitemapAsync()
        {
            string baseUrl = $"{Request.Scheme}://{Request.Host}{Url.Content("~")}";

            // get a list of public clubs
            var clubs = await _clubservice.GetClubs(false);

            var siteMapBuilder = new SitemapBuilder();
            siteMapBuilder.AddUrl(baseUrl +"/", changeFrequency: ChangeFrequency.Monthly, priority: 0.8);
            siteMapBuilder.AddUrl(baseUrl + Url.Action("News", "Home"), changeFrequency: ChangeFrequency.Monthly, priority: 0.5);
            siteMapBuilder.AddUrl(baseUrl + Url.Action("About", "Home"), changeFrequency: ChangeFrequency.Monthly, priority: 0.5);

            foreach (var club in clubs)
            {
                siteMapBuilder.AddUrl(baseUrl +"/"+ club.Initials, priority: 1.0);
            }

            // generate the sitemap xml
            string xml = siteMapBuilder.ToString();
            return Content(xml, "text/xml");
        }
    }
}

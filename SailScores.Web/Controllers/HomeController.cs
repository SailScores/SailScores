using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using CoreServices = SailScores.Core.Services;
using SailScores.Web.Models;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class HomeController : Controller
    {

        private readonly CoreServices.IClubService _clubservice;
        private readonly IRegattaService _regattaService;

        public HomeController(
            CoreServices.IClubService clubService,
            IRegattaService regattaService)

        {
            _clubservice = clubService;
            _regattaService = regattaService;
        }

        public async Task<IActionResult> Index()
        {
            var clubSelector = new ClubSelectorModel
            {
                Clubs = (await _clubservice.GetClubs(false))
                    .OrderBy(c => c.Name)
                    .ToList()
            };
            var regattaSelector = new RegattaSelectorModel
            {
                Regattas = (await _regattaService.GetCurrentRegattas()).ToList()
            };
            var model = new SiteHomePageModel
            {
                ClubSelectorModel = clubSelector,
                RegattaSelectorModel = regattaSelector
            };
            return View(model);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult News()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}

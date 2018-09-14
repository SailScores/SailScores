using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Sailscores.Web.Models.Sailscores;
using SailScores.Core.Services;
using SailScores.Web.Models;

namespace SailScores.Web.Controllers
{
    public class HomeController : Controller
    {

        private readonly IClubService _clubservice;

        public HomeController(
            IClubService clubService)
        {
            _clubservice = clubService;
        }

        public async Task<IActionResult> Index()
        {
            var model = new ClubSelectorModel
            {
                Clubs = (await _clubservice.GetClubs(false)).ToList()
            };
            return View(model);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

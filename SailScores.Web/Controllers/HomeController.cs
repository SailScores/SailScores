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
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
    public class HomeController : Controller
    {

        private readonly CoreServices.IClubService _clubservice;
        private readonly IRegattaService _regattaService;
        private readonly AppVersionInfo _versionService;

        public HomeController(
            CoreServices.IClubService clubService,
            IRegattaService regattaService,
            AppVersionInfo versionService)

        {
            _clubservice = clubService;
            _regattaService = regattaService;
            _versionService = versionService;
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
            var vm = new AboutViewModel
            {
                Version = _versionService.Version,
                Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                ShortGitHash = _versionService.ShortGitHash,
                GitHash = _versionService.GitHash,
                BuildId = _versionService.BuildId,
                BuildNumber = _versionService.BuildNumber
            };

#if DEBUG
            vm.Version = _versionService.InformationalVersion;
#endif
            return View(vm);
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

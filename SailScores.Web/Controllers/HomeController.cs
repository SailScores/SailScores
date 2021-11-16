using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Controllers;

[ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
public class HomeController : Controller
{

    private readonly CoreServices.IClubService _clubservice;
    private readonly IClubService _webClubService;
    private readonly IRegattaService _regattaService;
    private readonly AppVersionInfo _versionService;

    public HomeController(
        CoreServices.IClubService clubService,
        IClubService webClubService,
        IRegattaService regattaService,
        AppVersionInfo versionService)

    {
        _clubservice = clubService;
        _webClubService = webClubService;
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

    public async Task<IActionResult> Stats()
    {
        var vm = await _webClubService.GetAllClubStats();
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
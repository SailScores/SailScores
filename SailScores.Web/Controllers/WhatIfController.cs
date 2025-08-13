
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Identity.Entities;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;
using ISeriesService = SailScores.Web.Services.Interfaces.ISeriesService;

namespace SailScores.Web.Controllers;

[Authorize]
public class WhatIfController : Controller
{

    private readonly ISeriesService _seriesService;
    private readonly IWhatIfService _whatIfService;
    private readonly Core.Services.IClubService _clubService;
    private readonly IAuthorizationService _authService;
    private readonly IMapper _mapper;

    public WhatIfController(
        ISeriesService seriesService,
        Core.Services.IClubService clubService,
        IAuthorizationService authService,
        IWhatIfService whatIfService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _seriesService = seriesService;
        _whatIfService = whatIfService;
        _clubService = clubService;
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<ActionResult> Options(
        string clubInitials,
        Guid seriesId,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
        var series = await _seriesService.GetSeriesAsync(seriesId);

        var vm = new WhatIfViewModel
        {
            ScoringSystemOptions = await _whatIfService.GetScoringSystemOptions(clubId),
            Series = series,
            SeriesId = seriesId,
            Discards = series.FlatResults.NumberOfDiscards,
            SelectedScoringSystemId = series.ScoringSystemId,
        };

        return View(vm);

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Show(
        string clubInitials,
        WhatIfViewModel options,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
        //stripping unused user input for safety
        options.Series = null;
        options.ScoringSystemOptions = new List<Core.Model.ScoringSystem>();
        var vm = await _whatIfService.GetResults(options);
        return View("Results", vm);
    }
}
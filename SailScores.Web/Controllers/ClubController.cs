using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Controllers;

public class ClubController : Controller
{
    private readonly IClubService _clubService;
    private readonly IAuthorizationService _authService;
    private readonly IMapper _mapper;

    public ClubController(
        IClubService clubService,
        IAuthorizationService authService,
        IMapper mapper)
    {
        _clubService = clubService;
        _authService = authService;
        _mapper = mapper;
    }

    // This is the main page for clubs: lives at the url: sailscores.com/{clubinitials}
    public async Task<ActionResult> Index(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;

        var club = await _clubService.GetClubForClubHome(clubInitials);
        var viewModel = _mapper.Map<ClubSummaryViewModel>(club);
        viewModel.CanEdit = await _authService.CanUserEdit(User, clubInitials);
        return View(viewModel);
    }

    // GET: Club
    public async Task<ActionResult> Stats(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;

        var stats = await _clubService.GetClubStats(clubInitials);
        stats.CanEdit = await _authService.CanUserEdit(User, stats.Id);
        return View(stats);
    }

    // GET: Club
    public async Task<ActionResult> EditStats(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;

        var stats = await _clubService.GetClubStats(clubInitials);
        return View(stats);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Stats(string clubInitials,
        ClubStatsViewModel statsUpdate)
    {
        ViewData["ClubInitials"] = clubInitials;
        if (!await _authService.CanUserEdit(User, statsUpdate.Id))
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            return View("EditStats", statsUpdate);
        }

        // insert update description here.
        await _clubService.UpdateStatsDescription(clubInitials, statsUpdate.StatisticsDescription);
        var stats = await _clubService.GetClubStats(clubInitials);
        stats.CanEdit = true;
        return View(stats);
    }
}
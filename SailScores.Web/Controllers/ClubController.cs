using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
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
}

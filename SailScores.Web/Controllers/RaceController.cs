using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using Microsoft.AspNetCore.Identity;
using SailScores.Identity.Entities;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

public class RaceController : Controller
{

    private readonly Core.Services.IClubService _clubService;
    private readonly IRaceService _raceService;
    private readonly IAuthorizationService _authService;
    private readonly IAdminTipService _adminTipService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISpeechService _speechService;
    private readonly IMapper _mapper;

    public RaceController(
        Core.Services.IClubService clubService,
        IRaceService raceService,
        IAuthorizationService authService,
        IAdminTipService adminTipService,
        UserManager<ApplicationUser> userManager,
        ISpeechService speechService,
        IMapper mapper)
    {
        _clubService = clubService;
        _raceService = raceService;
        _authService = authService;
        _adminTipService = adminTipService;
        _userManager = userManager;
        _speechService = speechService;
        _mapper = mapper;
    }

    public async Task<ActionResult> Index(
        string clubInitials,
        string seasonName,
        bool showScheduled = true,
        bool showAbandoned = true)
    {
        
        var capInitials = clubInitials.ToUpperInvariant();
        if (String.IsNullOrWhiteSpace(seasonName))
        {
            var currentSeason = await _raceService.GetCurrentSeasonAsync(capInitials);
            if (currentSeason != null)
            {
                return RedirectToRoute("Race", new
                {
                    clubInitials = capInitials,
                    seasonName = currentSeason.UrlName
                });
            }
        }
        var clubName = await _clubService.GetClubName(capInitials);
        var races = await _raceService.GetAllRaceSummariesAsync(
            capInitials,
            seasonName,
            showScheduled,
            showAbandoned);
        
        if (races == null)
        {
            return NotFound();
        }

        return View(new ClubItemViewModel<RaceSummaryListViewModel>
        {
            Item = races,
            ClubInitials = capInitials,
            ClubName = clubName,
            CanEdit = await _authService.CanUserEdit(User, capInitials)
        });
    }

    public async Task<ActionResult> Details(string clubInitials, Guid id)
    {
        var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);

        if (race == null)
        {
            return NotFound();
        }
        var canEdit = false;
        if (User != null && (User.Identity?.IsAuthenticated ?? false))
        {
            canEdit = await _authService.CanUserEdit(User, clubInitials);
        }

        return View(new ClubItemViewModel<RaceViewModel>
        {
            Item = race,
            ClubInitials = clubInitials,
            CanEdit = canEdit
        });
    }

    [Authorize]
    public async Task<ActionResult> Create(
        string clubInitials,
        Guid? regattaId,
        Guid? seriesId,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        RaceWithOptionsViewModel race =
            await _raceService.GetBlankRaceWithOptions(
                clubInitials,
                regattaId,
                seriesId);
        var errors = _adminTipService.GetRaceCreateErrors(race);
        if (errors != null && errors.Count > 0)
        {
            return View("CreateErrors", errors);
        }
        _adminTipService.AddTips(ref race);
        return View(race);

    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<ActionResult> Create(
        string clubInitials,
        RaceWithOptionsViewModel race,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            race = await _raceService.FixupRaceWithOptions(clubInitials, race);

            return View(race);
        }
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEditRaces(User, clubId))
        {
            return Unauthorized();
        }
        race.ClubId = clubId;
        race.UpdatedBy = await GetUserStringAsync();
        await _raceService.SaveAsync(race);
        if (!string.IsNullOrWhiteSpace(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Admin");

    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user.GetDisplayName();
    }

    [Authorize]
    public async Task<ActionResult> Edit(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ClubInitials"] = clubInitials;
        var club = await _clubService.GetMinimalClub(clubInitials);
        if (!await _authService.CanUserEditRaces(User, club.Id))
        {
            return Unauthorized();
        }
        var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
        if (race == null)
        {
            return NotFound();
        }
        if (race.ClubId != club.Id)
        {
            return Unauthorized();
        }

        var raceWithOptions = _mapper.Map<RaceWithOptionsViewModel>(race);

        await _raceService.AddOptionsToRace(raceWithOptions);
        raceWithOptions.UseAdvancedFeatures = club.UseAdvancedFeatures ?? false;

        return View(raceWithOptions);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string clubInitials,
        Guid id,
        RaceWithOptionsViewModel race,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["ClubInitials"] = clubInitials;

        if (!await _authService.CanUserEditRaces(User, race.ClubId))
        {
            return Unauthorized();
        }
        if (!ModelState.IsValid)
        {
            race = await _raceService.FixupRaceWithOptions(clubInitials, race);

            return View(race);
        }

        race.UpdatedBy = await GetUserStringAsync();
        await _raceService.SaveAsync(race);

        return RedirectToLocal(returnUrl);
    }

    [Authorize]
    [HttpGet]
    public async Task<ActionResult> Delete(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEditRaces(User, clubId))
        {
            return Unauthorized();
        }
        var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
        if (race == null)
        {
            return NotFound();
        }
        if (race.ClubId != clubId)
        {
            return Unauthorized();
        }
        return View(race);
    }

    [Authorize]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        try
        {
            var race = await _raceService.GetSingleRaceDetailsAsync(clubInitials, id);
            if (!await _authService.CanUserEditRaces(User, clubInitials)
                || race == null)
            {
                return Unauthorized();
            }
            await _raceService.Delete(id, await GetUserStringAsync());

            return RedirectToAction("Index", "Race");
        }
        catch
        {
            return View();
        }
    }


    [Authorize]
    [HttpGet]
    [ActionName("SpeechInfo")]
    public async Task<SpeechInfo> GetSpeechInfo()
    {
        var user = await _userManager.GetUserAsync(User);
        return new SpeechInfo
        {
            Token = await _speechService.GetToken(),
            Region = _speechService.GetRegion(),
            UserLanguage = user.SpeechRecognitionLanguage ?? "en-US"
        };
    }

    private IActionResult RedirectToLocal(string returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction(nameof(AdminController.Index), "Admin");
        }
    }

}
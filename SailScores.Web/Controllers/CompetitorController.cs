using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Identity.Entities;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;
using IClubService = SailScores.Core.Services.IClubService;
using ICompetitorService = SailScores.Web.Services.Interfaces.ICompetitorService;
using IForwarderService = SailScores.Core.Services.IForwarderService;

namespace SailScores.Web.Controllers;

[Authorize]
public class CompetitorController : Controller
{
    private readonly IClubService _clubService;
    private readonly ICompetitorService _competitorService;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authService;
    private readonly ICsvService _csvService;
    private readonly IAdminTipService _adminTipService;
    private readonly IForwarderService _forwarderService;
    private readonly UserManager<ApplicationUser> _userManager;

    public CompetitorController(
        IClubService clubService,
        ICompetitorService competitorService,
        IForwarderService forwarderService,
        IAuthorizationService authService,
        ICsvService csvService,
        IAdminTipService adminTipService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper)
    {
        _clubService = clubService;
        _competitorService = competitorService;
        _forwarderService = forwarderService;
        _authService = authService;
        _csvService = csvService;
        _adminTipService = adminTipService;
        _userManager = userManager;
        _mapper = mapper;
    }

    [AllowAnonymous]
    // GET: Competitor
    public async Task<ActionResult> Index(
        string clubInitials,
        Guid? fleetId = null)
    {
        if (String.IsNullOrWhiteSpace(clubInitials))
        {
            return new NotFoundResult();
        }
        var canEdit = await _authService.CanUserEditRaces(User, clubInitials);
        var clubId = await _clubService.GetClubId(clubInitials);
        
        IList<CompetitorIndexViewModel> competitors;
        
        if (fleetId.HasValue)
        {
            var fleetCompetitors = await _competitorService.GetCompetitorsForFleetAsync(
                clubId, 
                fleetId.Value, 
                canEdit);
            var competitorList = fleetCompetitors.SelectMany(kvp => kvp.Value).ToList();
            competitors = _mapper.Map<IList<CompetitorIndexViewModel>>(competitorList);
        }
        else
        {
            competitors = await _competitorService
                .GetCompetitorsWithDeletableInfoAsync(clubInitials, canEdit);
        }
        
        var fleets = (await _clubService.GetAllFleets(clubId))
            .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats 
                     || f.FleetType == Api.Enumerations.FleetType.SelectedClasses)
            .OrderBy(f => f.Name)
            .ToList();
        
        var vm = new ClubCollectionViewModel<CompetitorIndexViewModel>
        {
            ClubInitials = clubInitials,
            List = competitors,
            CanEdit = await _authService.CanUserEditRaces(User, clubInitials),
            FleetOptions = _mapper.Map<List<FleetSummary>>(fleets),
            SelectedFleetId = fleetId
        };
        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    // {clubInitials}/Competitor/{urlName}
    public async Task<ActionResult> Details(string clubInitials, string urlName)
    {
        var competitorStats = await _competitorService.GetCompetitorStatsAsync(
            clubInitials,
            urlName);

        if (competitorStats == null)
        {
            var forward = await _forwarderService.GetCompetitorForwarding(clubInitials, urlName);
            if (forward != null)
            {
                return Redirect($"/{Uri.EscapeDataString(forward.NewClubInitials)}/Competitor/{Uri.EscapeDataString(forward.NewUrlName)}");
            }
            return NotFound();
        }

        // Get club weather settings for wind speed units
        var clubId = await _clubService.GetClubId(clubInitials);
        var club = await _clubService.GetFullClubExceptScores(clubId);
        var windSpeedUnits = club?.WeatherSettings?.WindSpeedUnits ?? "kts";

        var vm = new ClubItemViewModel<CompetitorStatsViewModel>
        {
            ClubInitials = clubInitials.ToUpperInvariant(),
            Item = competitorStats,
            WindSpeedUnits = windSpeedUnits
        };
        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<JsonResult> Chart(
        Guid competitorId,
        string seasonName)
    {
        var ranks = await _competitorService.GetCompetitorSeasonRanksAsync(
            competitorId,
            seasonName);
        if (ranks == null)
        {
            return Json(String.Empty);
        }
        return Json(ranks);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 86400, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "competitorId", "seasonName", "groupByDirection" })]
    public async Task<JsonResult> WindStats(
        Guid competitorId,
        string seasonName = null,
        bool groupByDirection = false)
    {
        var stats = await _competitorService.GetCompetitorWindStatsAsync(
            competitorId,
            seasonName,
            groupByDirection);

        if (stats == null || !stats.Any())
        {
            return Json(Array.Empty<object>());
        }

        return Json(stats);
    }


    // GET: Competitor/Create
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> Create(
        string clubInitials,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var comp = new CompetitorWithOptionsViewModel();

        var clubId = await _clubService.GetClubId(clubInitials);

        comp.BoatClassOptions = (await _clubService.GetAllBoatClasses(clubId))
            .OrderBy(c => c.Name);
        var fleets = (await _clubService.GetAllFleets(clubId))
            .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
            .OrderBy(f => f.Name);
        comp.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);

        var errors = _adminTipService.GetCompetitorCreateErrors(comp);
        if (errors != null && errors.Count > 0)
        {
            return View("CreateErrors", errors);
        }

        return View(comp);
    }

    // POST: Competitor/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> Create(
        string clubInitials,
        CompetitorWithOptionsViewModel competitor,
        string returnUrl = null)
#pragma warning restore CA1054 // Uri parameters should not be strings
    {
        ViewData["ReturnUrl"] = returnUrl;

        var clubId = await _clubService.GetClubId(clubInitials);
        competitor.ClubId = clubId;
        try
        {
            var fleets = (await _clubService.GetAllFleets(clubId))
                .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                .OrderBy(f => f.Name);
            if (!ModelState.IsValid)
            {
                competitor.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);
                competitor.BoatClassOptions = (await _clubService.GetAllBoatClasses(clubId))
                    .OrderBy(c => c.Name);
                return View(competitor);
            }

            competitor.Fleets = new List<Fleet>();
            foreach (var fleetId in (competitor.FleetIds ?? new List<Guid>()))
            {
                var fleet = fleets.SingleOrDefault(f => f.Id == fleetId);
                if (fleet != null)
                {
                    competitor.Fleets.Add(fleet);
                }
            }
            await _competitorService.SaveAsync(competitor,
                await GetUserStringAsync());
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Competitor");
        }
        catch
        {

            competitor.BoatClassOptions = (await _clubService.GetAllBoatClasses(clubId))
                .OrderBy(c => c.Name);
            var fleets = (await _clubService.GetAllFleets(clubId))
                .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                .OrderBy(f => f.Name);
            competitor.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);
            ModelState.AddModelError(String.Empty, "A problem occurred while saving.");

            return View(competitor);
        }
    }

    // GET: Competitor/CreateMultiple
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> CreateMultiple(
        string clubInitials,
        string returnUrl = null)
#pragma warning restore CA1054 // Uri parameters should not be strings
    {
        ViewData["ReturnUrl"] = returnUrl;
        var vm = new MultipleCompetitorsWithOptionsViewModel();
        var clubId = await _clubService.GetClubId(clubInitials);
        vm.BoatClassOptions = (await _clubService.GetAllBoatClasses(clubId))
            .OrderBy(c => c.Name);
        var fleets = (await _clubService.GetAllFleets(clubId))
            .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
            .OrderBy(f => f.Name);
        vm.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);

        var errors = _adminTipService.GetMultipleCompetitorsCreateErrors(vm);
        if (errors != null && errors.Count > 0)
        {
            return View("CreateErrors", errors);
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> CreateMultiple(
        string clubInitials,
        MultipleCompetitorsWithOptionsViewModel competitorsVm,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        var clubId = await _clubService.GetClubId(clubInitials);
        try
        {
            // we check for errors against previously saved competitors
            // but we don't check for errors against other competitors
            // currently being saved.
            int i = 0;
            foreach (var comp in competitorsVm.Competitors)
            {
                var compModel = _mapper.Map<Competitor>(comp);
                compModel.BoatClassId = competitorsVm.BoatClassId;
                compModel.ClubId = clubId;
                IEnumerable<KeyValuePair<string, string>> errors =
                    await _competitorService.GetSaveErrors(compModel);
                if (errors != null)
                {
                    foreach (var error in errors)
                    {
                        if (String.IsNullOrWhiteSpace(error.Key))
                        {
                            ModelState.AddModelError(string.Empty, error.Value);
                        }
                        else
                        {
                            ModelState.AddModelError($"Competitors[{i}].{error.Key}", error.Value);
                        }
                    }
                }

                i++;
            }

            if (!ModelState.IsValid)
            {
                var fleets = (await _clubService.GetAllFleets(clubId))
                    .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                    .OrderBy(f => f.Name);
                competitorsVm.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);

                competitorsVm.BoatClassOptions = (await _clubService.GetAllBoatClasses(clubId))
                    .OrderBy(c => c.Name);
                return View(competitorsVm);
            }

            string username = await GetUserStringAsync();
            await _competitorService.SaveAsync(competitorsVm, clubId, username);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Competitor");
        }
        catch
        {
            var fleets = (await _clubService.GetAllFleets(clubId))
                .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                .OrderBy(f => f.Name);
            competitorsVm.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);

            competitorsVm.BoatClassOptions = (await _clubService.GetAllBoatClasses(clubId))
                .OrderBy(c => c.Name);
            return View(competitorsVm);
        }
    }

    // GET: Competitor/Edit/5
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> Edit(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        var compWithOptions = await _competitorService.GetCompetitorWithHistoryAsync(id);
        if (compWithOptions == null)
        {
            return NotFound();
        }
        if (compWithOptions.ClubId != clubId)
        {
            return Unauthorized();
        }

        compWithOptions.BoatClassOptions =
            (await _clubService.GetAllBoatClasses(clubId))
            .OrderBy(c => c.Name);
        var fleets = (await _clubService.GetAllFleets(clubId))
            .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
            .OrderBy(f => f.Name);
        compWithOptions.FleetOptions = _mapper.Map<IList<FleetSummary>>(fleets);

        return View(compWithOptions);
    }

    // POST: Competitor/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> Edit(
        Guid id,
        CompetitorWithOptionsViewModel competitor)
    {
        try
        {

            IEnumerable<KeyValuePair<string, string>> errors =
                await _competitorService.GetSaveErrors(competitor);
            if(errors != null)
            {
                foreach(var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            if (!ModelState.IsValid)
            {
                competitor.BoatClassOptions =
                    (await _clubService.GetAllBoatClasses(competitor.ClubId))
                    .OrderBy(c => c.Name);
                var fleets =
                    (await _clubService.GetAllFleets(competitor.ClubId))
                    .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                    .OrderBy(f => f.Name);
                competitor.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);
                return View(competitor);
            }
            await _competitorService.SaveAsync(competitor, await GetUserStringAsync());

            return RedirectToAction("Index", "Competitor");
        }
        catch
        {
            ModelState.AddModelError(String.Empty,
                "An error occurred editing this competitor.");
            competitor.BoatClassOptions =
                (await _clubService.GetAllBoatClasses(competitor.ClubId))
                .OrderBy(c => c.Name);
                        var fleets =
                            (await _clubService.GetAllFleets(competitor.ClubId))
                            .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                            .OrderBy(f => f.Name);
                        competitor.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);
            return View(competitor);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveNote(CompetitorWithOptionsViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.NewNote))
        {
            ModelState.AddModelError("NewNote", "Note cannot be empty.");
            // Reload the edit page with validation error
            return RedirectToAction("Edit", new { id = model.Id });
        }

        await _competitorService.AddCompetitorNote(model.Id, model.NewNote, await GetUserStringAsync());

        // Redirect back to the edit page
        return RedirectToAction("Edit", new { id = model.Id });
    }

    [HttpGet]
    // GET: Competitor/Delete/5
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> Delete(
        string clubInitials,
        Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var competitor = await _competitorService.GetCompetitorAsync(id);
        if (competitor == null)
        {
            return NotFound();
        }
        if (competitor.ClubId != clubId)
        {
            return Unauthorized();
        }
        return View(competitor);
    }

    // POST: Competitor/Delete/5
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var competitor = await _competitorService.GetCompetitorAsync(id);
            if (competitor.ClubId != clubId)
            {
                return Unauthorized();
            }
            await _competitorService.DeleteCompetitorAsync(id);

            return RedirectToAction("Index", "Competitor");
        }
        catch
        {
            ModelState.AddModelError(String.Empty,
                "An error occurred deleting this competitor. Are they assigned scores in existing races?");
            var competitor = await _competitorService.GetCompetitorAsync(id);
            return View(competitor);
        }
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    // GET: Competitor/InactivateMultiple
    public async Task<ActionResult> InactivateMultiple(string clubInitials)
    {
        return View();
    }

    // POST: Competitor/InactivateAll
    [HttpPost]
    [ActionName("InactivateAll")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> PostInactivateMultiple(
        string clubInitials,
        [FromForm] DateTime sinceDate)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEditRaces(User, clubId))
        {
            return Unauthorized();
        }
        await _competitorService.InactivateSince(clubId, sinceDate);

        return RedirectToAction("Index", "Competitor");
    }

	[HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    // GET: Competitor/ClearAltNumbers
    public async Task<ActionResult> ClearAltNumbers(string clubInitials)
	{
		return View();
	}

	// POST: Competitor/ClearAltNumbers
	[HttpPost]
	[ActionName("ClearAltNumbers")]
	[ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.SeriesScorekeeper)]
    public async Task<ActionResult> PostClearAltNumbers(string clubInitials)
	{
        var clubId = await _clubService.GetClubId(clubInitials);

        await _competitorService.ClearAltNumbers(clubId);

		return RedirectToAction("Index", "Competitor");
	}

    [HttpPost]
    [ActionName("SetCompActive")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> PostCompActive(
        string clubInitials,
        CompetitorActivateViewModel compActivate)
    {
        var clubId = await _clubService.GetClubId(clubInitials);

        await _competitorService.SetCompetitorActive(clubId, 
            compActivate.CompetitorId,
            compActivate.Active,
            await GetUserStringAsync());
        return new JsonResult(new object()); ;

    }

	[HttpGet]
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> Utilities(
        		string clubInitials,
                		string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
		var clubId = await _clubService.GetClubId(clubInitials);

		var competitors = await _competitorService
			.GetCompetitorsAsync(clubInitials, true);
        var compVm = _mapper.Map<List<CompetitorIndexViewModel>>(competitors);
		var vm = new ClubCollectionViewModel<CompetitorIndexViewModel>
        {
			ClubInitials = clubInitials,
			List = compVm,
			CanEdit = await _authService.CanUserEditRaces(User, clubInitials)
		};
		return View(vm);
	}

    public async Task<ActionResult> ExportCsv(
        string clubInitials,
        string fleetId = default,
        string regattaId = default,
        bool includeInactive = false)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        IDictionary<string, IEnumerable<Competitor>> competitors = null;
        try
        {
            if (fleetId != default)
            {
                competitors = await _competitorService.GetCompetitorsForFleetAsync(clubId, new Guid(fleetId), includeInactive);
            }
            else if (regattaId != default)
            {
                competitors = await _competitorService.GetCompetitorsForRegattaAsync(clubId, new Guid(regattaId), includeInactive);
            }
            else
            {
                var allCompetitors = await _competitorService.GetCompetitorsAsync(clubId, includeInactive);
                competitors = new Dictionary<string, IEnumerable<Competitor>>
                {
                    { clubInitials, allCompetitors }
                };
            }
        }
        catch (InvalidOperationException)
        {
            competitors = null;
        }
        if (competitors == null)
        {
            return new NotFoundResult();
        }

        var filename = competitors.Count != 1 ? "competitors.csv" : $"{competitors.First().Key}.csv";
        var csv = _csvService.GetCsv(competitors);

        return File(csv, "text/csv", filename);
    }

    public async Task<ActionResult> ExportHtml(
        string clubInitials,
        string fleetId = default,
        string regattaId = default,
        bool includeInactive = false)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        IDictionary<string, IEnumerable<Competitor>> competitors = null;
        try
        {
            if (fleetId != default)
            {
                competitors = await _competitorService.GetCompetitorsForFleetAsync(clubId, new Guid(fleetId), includeInactive);
            }
            else if (regattaId != default)
            {
                competitors = await _competitorService.GetCompetitorsForRegattaAsync(clubId, new Guid(regattaId), includeInactive);
            }
            else
            {
                var allCompetitors = await _competitorService.GetCompetitorsAsync(clubId, includeInactive);
                competitors = new Dictionary<string, IEnumerable<Competitor>>
                {
                    { clubInitials, allCompetitors }
                };
            }
        }
        catch (InvalidOperationException)
        {
            competitors = null;
        }
        if (competitors == null)
        {
            return new NotFoundResult();
        }

        var filename = competitors.Count != 1 ? "competitors.html" : $"{competitors.First().Key}.html";
        var disposition = $"attachment; filename=\"{filename}\"; filename*=UTF-8''{filename}";
        Response.Headers.Append("content-disposition", disposition);

        return View(competitors);
    }

    private async Task<string> GetUserStringAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        return user.GetDisplayName();
    }

}

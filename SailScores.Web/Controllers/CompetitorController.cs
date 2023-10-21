using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;
using IClubService = SailScores.Core.Services.IClubService;
using ICompetitorService = SailScores.Web.Services.Interfaces.ICompetitorService;

namespace SailScores.Web.Controllers;

[Authorize]
public class CompetitorController : Controller
{
    private readonly IClubService _clubService;
    private readonly ICompetitorService _competitorService;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authService;
    private readonly IAdminTipService _adminTipService;

    public CompetitorController(
        IClubService clubService,
        ICompetitorService competitorService,
        IAuthorizationService authService,
        IAdminTipService adminTipService,
        IMapper mapper)
    {
        _clubService = clubService;
        _competitorService = competitorService;
        _authService = authService;
        _adminTipService = adminTipService;
        _mapper = mapper;
    }

    [AllowAnonymous]
    // GET: Competitor
    public async Task<ActionResult> Index(string clubInitials)
    {
        var canEdit = await _authService.CanUserEdit(User, clubInitials);
        var competitors = await _competitorService
            .GetCompetitorsWithDeletableInfoAsync(clubInitials, canEdit);
        var vm = new ClubCollectionViewModel<CompetitorIndexViewModel>
        {
            ClubInitials = clubInitials,
            List = competitors,
            CanEdit = await _authService.CanUserEdit(User, clubInitials)
        };
        return View(vm);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    // {clubInitials}/Competitor/{sailNumber}
    public async Task<ActionResult> Details(string clubInitials, string sailNumber)
    {
        var competitorStats = await _competitorService.GetCompetitorStatsAsync(
            clubInitials,
            sailNumber);
        if (competitorStats == null)
        {
            return new NotFoundResult();
        }
        var vm = new ClubItemViewModel<CompetitorStatsViewModel>
        {
            ClubInitials = clubInitials.ToUpperInvariant(),
            Item = competitorStats
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


    // GET: Competitor/Create
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
    public async Task<ActionResult> Create(
        string clubInitials,
        CompetitorWithOptionsViewModel competitor,
#pragma warning disable CA1054 // Uri parameters should not be strings
        string returnUrl = null)
#pragma warning restore CA1054 // Uri parameters should not be strings
    {
        ViewData["ReturnUrl"] = returnUrl;

        var clubId = await _clubService.GetClubId(clubInitials);
            
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
        competitor.ClubId = clubId;
        try
        {
            var fleets = (await _clubService.GetAllFleets(clubId))
                .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                .OrderBy(f => f.Name);
            if (!ModelState.IsValid)
            {
                competitor.FleetOptions = _mapper.Map<List<FleetSummary>>(fleets);
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
            await _competitorService.SaveAsync(competitor);
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
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
    public async Task<ActionResult> CreateMultiple(
        string clubInitials,
#pragma warning disable CA1054 // Uri parameters should not be strings
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
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }

            await _competitorService.SaveAsync(competitorsVm, clubId);

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
    public async Task<ActionResult> Edit(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }

        var competitor = await _competitorService.GetCompetitorAsync(id);
        if (competitor == null)
        {
            return NotFound();
        }
        if (competitor.ClubId != clubId)
        {
            return Unauthorized();
        }
        var compWithOptions = _mapper.Map<CompetitorWithOptionsViewModel>(competitor);

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
    public async Task<ActionResult> Edit(
        Guid id,
        CompetitorWithOptionsViewModel competitor)
    {
        try
        {
            if (!await _authService.CanUserEdit(User, competitor.ClubId))
            {
                return Unauthorized();
            }

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
            await _competitorService.SaveAsync(competitor);

            return RedirectToAction("Index", "Competitor");
        }
        catch
        {
            return View();
        }
    }

    [HttpGet]
    // GET: Competitor/Delete/5
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
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
    public async Task<ActionResult> PostDelete(string clubInitials, Guid id)
    {
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            var competitor = await _competitorService.GetCompetitorAsync(id);
            if (!await _authService.CanUserEdit(User, clubId)
                || competitor.ClubId != clubId)
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
    // GET: Competitor/InactivateMultiple
    public async Task<ActionResult> InactivateMultiple(string clubInitials)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }
        return View();
    }

    // POST: Competitor/InactivateAll
    [HttpPost]
    [ActionName("InactivateAll")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostInactivateMultiple(
        string clubInitials,
        [FromForm] DateTime sinceDate)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
        {
            return Unauthorized();
        }
        await _competitorService.InactivateSince(clubId, sinceDate);

        return RedirectToAction("Index", "Competitor");
    }

	[HttpGet]
	// GET: Competitor/ClearAltNumbers
	public async Task<ActionResult> ClearAltNumbers(string clubInitials)
	{
		if (!await _authService.CanUserEdit(User, clubInitials))
		{
			return Unauthorized();
		}
		return View();
	}

	// POST: Competitor/ClearAltNumbers
	[HttpPost]
	[ActionName("ClearAltNumbers")]
	[ValidateAntiForgeryToken]
	public async Task<ActionResult> PostClearAltNumbers(string clubInitials)
	{

        var clubId = await _clubService.GetClubId(clubInitials);
        if (!await _authService.CanUserEdit(User, clubId))
		{
			return Unauthorized();
		}
        await _competitorService.ClearAltNumbers(clubId);

		return RedirectToAction("Index", "Competitor");
	}

	[HttpGet]
    public async Task<ActionResult> Utilities(
        		string clubInitials,
                		string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
		var clubId = await _clubService.GetClubId(clubInitials);
		if (!await _authService.CanUserEdit(User, clubId))
        {
			return Unauthorized();
		}
		var competitors = await _competitorService
			.GetCompetitorsAsync(clubInitials, true);
        var compVm = _mapper.Map<List<CompetitorIndexViewModel>>(competitors);
		var vm = new ClubCollectionViewModel<CompetitorIndexViewModel>
        {
			ClubInitials = clubInitials,
			List = compVm,
			CanEdit = await _authService.CanUserEdit(User, clubInitials)
		};
		return View(vm);
	}
}
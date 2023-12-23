using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

public class FleetController : Controller
{


    private readonly CoreServices.IClubService _clubService;
    private readonly IFleetService _fleetService;
    private readonly IMapper _mapper;
    private readonly IAuthorizationService _authService;

    public FleetController(
        CoreServices.IClubService clubService,
        IFleetService fleetService,
        IAuthorizationService authService,
        IMapper mapper)
    {
        _clubService = clubService;
        _fleetService = fleetService;
        _authService = authService;
        _mapper = mapper;
    }

    public async Task<ActionResult> Index(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;

        var fleet = await _fleetService.GetAllFleetSummary(clubInitials);

        return View(new ClubCollectionViewModel<FleetSummary>
        {
            List = fleet,
            ClubInitials = clubInitials
        });
    }

    public async Task<ActionResult> Details(
        string clubInitials,
        string fleetShortName)
    {
        ViewData["ClubInitials"] = clubInitials;

        var fleet = await _fleetService.GetFleet(clubInitials, fleetShortName);

        return View(new ClubItemViewModel<FleetSummary>
        {
            Item = fleet,
            ClubInitials = clubInitials
        });
    }

    public async Task<ActionResult> Create(
        string clubInitials,
        Guid? regattaId,
        string returnUrl = null)
    {

        ViewData["ReturnUrl"] = returnUrl;
        var vm = await _fleetService.GetBlankFleetWithOptionsAsync(
            clubInitials,
            regattaId);

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Create(
        string clubInitials,
        FleetWithOptionsViewModel model,
        string returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        try
        {
            var clubId = await _clubService.GetClubId(clubInitials);
            if (!await _authService.CanUserEdit(User, clubId))
            {
                return Unauthorized();
            }
            model.ClubId = clubId;
            if (!ModelState.IsValid)
            {

                var vmOptions = await _fleetService.GetBlankFleetWithOptionsAsync(
                    clubInitials,
                    model.RegattaId);
                model.BoatClassOptions = vmOptions.BoatClassOptions;
                model.CompetitorOptions = vmOptions.CompetitorOptions;
                model.CompetitorBoatClassOptions = vmOptions.CompetitorBoatClassOptions;

                return View(model);
            }
            await _fleetService.SaveNew(model);
            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            ModelState.AddModelError(String.Empty,
                "An error occurred creating this fleet. Is the fleet name already in use in the club?");
            var vm = await _fleetService.GetBlankFleetWithOptionsAsync(
                clubInitials,
                model.RegattaId);
            model.BoatClassOptions = vm.BoatClassOptions;
            return View(model);
        }
    }

    [Authorize]
    public async Task<ActionResult> Edit(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        try
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }
            var fleet =
                await _fleetService.GetFleet(id);
            var vmOptions = await _fleetService.GetBlankFleetWithOptionsAsync(
                clubInitials,
                null);
            var vm = _mapper.Map<FleetWithOptionsViewModel>(fleet);
            vm.BoatClassOptions = vmOptions.BoatClassOptions;
            vm.CompetitorOptions = vmOptions.CompetitorOptions;
            vm.CompetitorBoatClassOptions = vmOptions.CompetitorBoatClassOptions;

            return View(vm);
        }
        catch
        {
            return RedirectToAction("Index", "Admin");
        }
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(
        string clubInitials,
        FleetWithOptionsViewModel model,
        string returnUrl = null)
    {
        try
        {
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid)
            {

                var vmOptions = await _fleetService.GetBlankFleetWithOptionsAsync(
                    clubInitials,
                    model.RegattaId);
                model.BoatClassOptions = vmOptions.BoatClassOptions;
                model.CompetitorOptions = vmOptions.CompetitorOptions;
                model.CompetitorBoatClassOptions = vmOptions.CompetitorBoatClassOptions;

                return View(model);
            }
            await _fleetService.Update(model);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            return View(model);
        }
    }

    [Authorize]
    public async Task<ActionResult> Delete(string clubInitials, Guid id)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }
        var fleet = await _fleetService.GetFleet(id);
        return View(fleet);
    }

    [Authorize]
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> PostDelete(
        string clubInitials,
        Guid id,
        string returnUrl = null)
    {
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }
        try
        {
            await _fleetService.Delete(id);

            if (!string.IsNullOrWhiteSpace(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Admin");
        }
        catch
        {
            var fleet = await _fleetService.GetFleet(id);
            return View(fleet);
        }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Authorization;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class AdminController : Controller
{
    private const string ClubInitialsVarName = "ClubInitials";
    private readonly IAdminService _adminService;
    private readonly IAuthorizationService _authService;
    private readonly IAdminTipService _tipService;
    private readonly IMapper _mapper;

    public AdminController(
        IAdminService adminService,
        IAuthorizationService authService,
        IAdminTipService tipService,
        IMapper mapper)
    {
        _adminService = adminService;
        _authService = authService;
        _tipService = tipService;
        _mapper = mapper;
    }

    // GET: Admin
    [Authorize(Policy = AuthorizationPolicies.RaceScorekeeper)]
    public async Task<ActionResult> Index(string clubInitials)
    {
        ViewData[ClubInitialsVarName] = clubInitials;

        var vm = await _adminService.GetClub(clubInitials);

        // Set permission flags for UI visibility
        vm.IsClubAdmin = await _authService.IsUserClubAdministrator(User, vm.Id)
                         || await _authService.IsUserFullAdmin(User);
        vm.CanEditSeries = await _authService.CanUserEditSeries(User, vm.Id)
                           || vm.IsClubAdmin;

        _tipService.AddTips(ref vm);
        return View(vm);
    }


    // GET: Admin/Edit
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(string clubInitials)
    {
        ViewData[ClubInitialsVarName] = clubInitials;
        var vm = await _adminService.GetClubForEdit(clubInitials);
        var editVm = _mapper.Map<AdminEditViewModel>(vm);
        editVm.RaceCount = await _adminService.GetRaceCountAsync(vm.Id);
        return View(editVm);
    }

    // POST: Admin/Edit
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> Edit(
        string clubInitials,
        AdminEditViewModel clubAdmin)
    {
        ViewData[ClubInitialsVarName] = clubInitials;
        try
        {
            if (!ModelState.IsValid)
            {
                var club = await _adminService.GetClubForEdit(clubInitials);
                clubAdmin.Seasons = club.Seasons;
                clubAdmin.ScoringSystemOptions = club.ScoringSystemOptions;
                clubAdmin.SpeedUnitOptions = club.SpeedUnitOptions;
                clubAdmin.TemperatureUnitOptions = club.TemperatureUnitOptions;
                clubAdmin.LocaleOptions = club.LocaleOptions;
                clubAdmin.DefaultRaceDateOffset = club.DefaultRaceDateOffset;
                return View(clubAdmin);
            }

            try
            {
                // Process logo file upload if provided
                await _adminService.ProcessLogoFile(clubAdmin);
            } catch (Exception ex)
            {
                ModelState.AddModelError("LogoFile", $"Error processing logo file: {ex.Message}");
                var club = await _adminService.GetClubForEdit(clubInitials);
                clubAdmin.Seasons = club.Seasons;
                clubAdmin.ScoringSystemOptions = club.ScoringSystemOptions;
                clubAdmin.SpeedUnitOptions = club.SpeedUnitOptions;
                clubAdmin.TemperatureUnitOptions = club.TemperatureUnitOptions;
                clubAdmin.LocaleOptions = club.LocaleOptions;
                clubAdmin.DefaultRaceDateOffset = club.DefaultRaceDateOffset;
                clubAdmin.LogoFile = null; // Clear the file input
                return View(clubAdmin);
            }

            var clubObject = _mapper.Map<Club>(clubAdmin);
            clubObject.DefaultScoringSystemId =
                clubAdmin.DefaultScoringSystemId;
            clubObject.WeatherSettings = new WeatherSettings
            {
                Latitude = clubAdmin.Latitude,
                Longitude = clubAdmin.Longitude,
                TemperatureUnits = clubAdmin.TemperatureUnits,
                WindSpeedUnits = clubAdmin.SpeedUnits
            };
            clubObject.Initials = clubInitials;

            await _adminService.UpdateClub(clubObject);

            return RedirectToAction(nameof(Index), "Admin", new { clubInitials });
        }
        catch
        {
            var club = await _adminService.GetClubForEdit(clubInitials);
            clubAdmin.Seasons = club.Seasons;
            clubAdmin.ScoringSystemOptions = club.ScoringSystemOptions;
            clubAdmin.SpeedUnitOptions = club.SpeedUnitOptions;
            clubAdmin.TemperatureUnitOptions = club.TemperatureUnitOptions;
            return View(clubAdmin);
        }
    }

    [AllowAnonymous]
    public async Task<IActionResult> GetLogo(Guid id)
    {
        var result = await _adminService.GetLogoAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return result;
    }

    // GET: Admin/ResetClub
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> ResetClub(string clubInitials)
    {
        ViewData[ClubInitialsVarName] = clubInitials;
        var club = await _adminService.GetClub(clubInitials);
        var raceCount = await _adminService.GetRaceCountAsync(club.Id);
        var vm = new ResetClubViewModel
        {
            ClubId = club.Id,
            ClubName = club.Name,
            ClubInitials = club.Initials,
            RaceCount = raceCount
        };
        return View(vm);
    }

    // POST: Admin/ResetClub
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AuthorizationPolicies.ClubAdmin)]
    public async Task<ActionResult> ResetClub(
        string clubInitials,
        ResetClubViewModel model)
    {
        ViewData[ClubInitialsVarName] = clubInitials;
        
        // Re-fetch race count to prevent manipulation
        var raceCount = await _adminService.GetRaceCountAsync(model.ClubId);
        model.RaceCount = raceCount;
        
        if (!model.CanSelfReset)
        {
            ModelState.AddModelError(string.Empty, 
                $"This club has {raceCount} races which exceeds the self-service limit of {ResetClubViewModel.MaxSelfServiceRaceCount}. Please contact info@sailscores.com to request a reset.");
            return View(model);
        }
        
        if (!ModelState.IsValid || !model.ResetLevel.HasValue)
        {
            return View(model);
        }

        try
        {
            await _adminService.ResetClubAsync(model.ClubId, model.ResetLevel.Value);
            TempData["SuccessMessage"] = $"Club data has been reset successfully using '{GetResetLevelDescription(model.ResetLevel.Value)}' option.";
            return RedirectToAction(nameof(Index), "Admin", new { clubInitials });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Error resetting club: {ex.Message}");
            return View(model);
        }
    }

    private static string GetResetLevelDescription(Core.Model.ResetLevel level)
    {
        return level switch
        {
            Core.Model.ResetLevel.RacesAndSeries => "Clear Races and Series",
            Core.Model.ResetLevel.RacesSeriesAndCompetitors => "Clear Races, Series, and Competitors",
            Core.Model.ResetLevel.FullReset => "Full Reset",
            _ => level.ToString()
        };
    }

}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using IAuthorizationService = SailScores.Web.Services.Interfaces.IAuthorizationService;

namespace SailScores.Web.Controllers;

[Authorize]
public class AdminController : Controller
{

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
    public async Task<ActionResult> Index(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }
        var vm = await _adminService.GetClub(clubInitials);

        _tipService.AddTips(ref vm);
        return View(vm);
    }


    // GET: Admin/Edit/LHYC
    public async Task<ActionResult> Edit(string clubInitials)
    {
        ViewData["ClubInitials"] = clubInitials;
        if (!await _authService.CanUserEdit(User, clubInitials))
        {
            return Unauthorized();
        }
        var vm = await _adminService.GetClubForEdit(clubInitials);
        return View(vm);
    }

    // POST: Admin/Edit/LHYC
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> Edit(
        string clubInitials,
        AdminViewModel clubAdmin)
    {
        ViewData["ClubInitials"] = clubInitials;
        try
        {
            if (!await _authService.CanUserEdit(User, clubAdmin.Id))
            {
                return Unauthorized();
            }
            if (!ModelState.IsValid)
            {
                var club = await _adminService.GetClubForEdit(clubInitials);
                clubAdmin.Seasons = club.Seasons;
                clubAdmin.ScoringSystemOptions = club.ScoringSystemOptions;
                clubAdmin.SpeedUnitOptions = club.SpeedUnitOptions;
                clubAdmin.TemperatureUnitOptions = club.TemperatureUnitOptions;
                clubAdmin.LocaleOptions = club.LocaleOptions;
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
            clubObject.Locale = _adminService.GetLocaleShortName(clubAdmin.Locale);
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

}
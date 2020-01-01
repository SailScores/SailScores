using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using CoreServices = SailScores.Core.Services;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {

        private readonly CoreServices.IClubService _clubService;
        private readonly CoreServices.IScoringService _scoringService;
        private readonly Services.IAuthorizationService _authService;
        private readonly Services.IAdminTipService _tipService;
        private readonly Services.IWeatherService _weatherService;
        private readonly IMapper _mapper;

        public AdminController(
            CoreServices.IClubService clubService,
            CoreServices.IScoringService scoringService,
            Services.IAuthorizationService authService,
            Services.IAdminTipService tipService,
            Services.IWeatherService weatherService,
            IMapper mapper)
        {
            _clubService = clubService;
            _scoringService = scoringService;
            _authService = authService;
            _tipService = tipService;
            _weatherService = weatherService;
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
            var club = await _clubService.GetFullClub(clubInitials);

            var vm = _mapper.Map<AdminViewModel>(club);
            vm.ScoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);

            _tipService.AddTips(ref vm);
            return View(vm);
        }

       
        // GET: Admin/Edit/5
        public async Task<ActionResult> Edit(string clubInitials)
        {
            ViewData["ClubInitials"] = clubInitials;
            if (!await _authService.CanUserEdit(User, clubInitials))
            {
                return Unauthorized();
            }

            var club = await _clubService.GetFullClub(clubInitials);

            var vm = _mapper.Map<AdminViewModel>(club);
            vm.ScoringSystemOptions = await _scoringService.GetScoringSystemsAsync(club.Id, true);
            vm.SpeedUnitOptions = _weatherService.GetSpeedUnitOptions();
            vm.TemperatureUnitOptions = _weatherService.GetTemperatureUnitOptions();

            return View(vm);
        }

        // POST: Admin/Edit/5
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
                    var club = await _clubService.GetFullClub(clubInitials);
                    clubAdmin.Seasons = club.Seasons;
                    clubAdmin.Races = club.Races;
                    clubAdmin.ScoringSystemOptions = await _scoringService.GetScoringSystemsAsync(clubAdmin.Id, true);
                    return View(clubAdmin);
                }
                var clubObject = _mapper.Map<Club>(clubAdmin);
                clubObject.DefaultScoringSystemId =
                    clubAdmin.DefaultScoringSystemId ?? clubAdmin?.DefaultScoringSystem?.Id;
                clubObject.WeatherSettings = new WeatherSettings
                {
                    Latitude = clubAdmin.Latitude,
                    Longitude = clubAdmin.Longitude,
                    TemperatureUnits = clubAdmin.TemperatureUnits,
                    WindSpeedUnits = clubAdmin.SpeedUnits
                };

                await _clubService.UpdateClub(clubObject);

                return RedirectToAction(nameof(Index), "Admin", new { clubInitials = clubInitials });
            }
            catch
            {
                var club = await _clubService.GetFullClub(clubInitials);
                clubAdmin.Seasons = club.Seasons;
                clubAdmin.Races = club.Races;
                clubAdmin.ScoringSystemOptions = await _scoringService.GetScoringSystemsAsync(clubAdmin.Id, true);
                return View(clubAdmin);
            }
        }

    }
}
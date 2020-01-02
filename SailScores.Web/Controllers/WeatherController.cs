using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Services;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;

namespace SailScores.Web.Controllers
{
    public class WeatherController : Controller
    {

        private readonly Core.Services.IClubService _clubService;
        private readonly Web.Services.IWeatherService  _weatherService;
        private readonly Services.IAuthorizationService _authService;
        private readonly Services.IAdminTipService _adminTipService;
        private readonly IMapper _mapper;

        public WeatherController(
            Core.Services.IClubService clubService,
            Web.Services.IWeatherService weatherService,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _clubService = clubService;
            _weatherService = weatherService;
            _authService = authService;
            _mapper = mapper;
        }


        [Authorize]
        public async Task<JsonResult> Current(string clubInitials)
        {

            var clubId = await _clubService.GetClubId(clubInitials);
            var weatherVm = await _weatherService.GetCurrentWeatherForClubAsync(clubId);
            return Json(weatherVm);
        }

    }
}
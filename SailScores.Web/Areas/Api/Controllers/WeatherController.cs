using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Areas.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = AuthSchemes)]
    public class WeatherController : ControllerBase
    {
        private const string AuthSchemes =
            CookieAuthenticationDefaults.AuthenticationScheme + "," +
            JwtBearerDefaults.AuthenticationScheme;

        private readonly Services.IWeatherService _service;

        public WeatherController(
            Services.IWeatherService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<WeatherViewModel> Get(
            Guid clubId
            )
        {
            var result =  await _service.GetCurrentWeatherForClubAsync(clubId);
            return result;
        }
    }
}

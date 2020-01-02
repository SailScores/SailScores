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
        private readonly Services.IAuthorizationService _authService;
        private readonly IMapper _mapper;

        public WeatherController(
            Services.IWeatherService service,
            Services.IAuthorizationService authService,
            IMapper mapper)
        {
            _service = service;
            _authService = authService;
            _mapper = mapper;
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

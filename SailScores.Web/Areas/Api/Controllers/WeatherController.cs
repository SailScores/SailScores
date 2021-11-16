using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

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

        private readonly IWeatherService _service;

        public WeatherController(
            IWeatherService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<WeatherViewModel> Get(
            Guid clubId
            )
        {
            var result = await _service.GetCurrentWeatherForClubAsync(clubId);
            return result;
        }
    }
}

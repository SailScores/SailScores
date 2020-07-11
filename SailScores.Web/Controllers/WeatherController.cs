using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SailScores.Web.Controllers
{
    public class WeatherController : Controller
    {

        private readonly Core.Services.IClubService _clubService;
        private readonly Web.Services.IWeatherService  _weatherService;

        public WeatherController(
            Core.Services.IClubService clubService,
            Web.Services.IWeatherService weatherService)
        {
            _clubService = clubService;
            _weatherService = weatherService;
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
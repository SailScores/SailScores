using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Controllers;

public class WeatherController : Controller
{

    private readonly Core.Services.IClubService _clubService;
    private readonly IWeatherService _weatherService;

    public WeatherController(
        Core.Services.IClubService clubService,
        IWeatherService weatherService)
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
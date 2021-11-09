using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IWeatherService
{
    Task<WeatherViewModel> GetCurrentWeatherForClubAsync(Guid clubId);
    IList<KeyValuePair<string, string>> GetWeatherIconOptions();
    Task<WeatherViewModel> ConvertToLocalizedWeather(Weather weather, Guid clubId);
    string GetIconCharacter(string iconName);
    Weather GetStandardWeather(WeatherViewModel weather);
    IList<string> GetSpeedUnitOptions();
    IList<string> GetTemperatureUnitOptions();
    Task<WeatherViewModel> GetCurrentWeatherForClubAsync(Club club);
}
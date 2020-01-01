using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IWeatherService
    {
        Task<WeatherViewModel> GetCurrentWeatherForClubAsync(Guid clubId);
        IList<KeyValuePair<string, string>> GetWeatherIconOptions();
        Task<WeatherViewModel> ConvertToLocalizedWeather(Weather weather, Guid clubId);
        string GetIconCharacter(string iconName);
        Weather GetStandardWeather(WeatherViewModel weather);
        IList<string> GetSpeedUnitOptions();
        IList<string> GetTemperatureUnitOptions();
    }
}
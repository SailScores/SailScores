using AutoMapper;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core = SailScores.Core;

namespace SailScores.Web.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly Core.Services.IClubService _coreClubService;
        private readonly Core.Services.IWeatherService _coreWeatherService;
        private readonly Core.Services.IConversionService _converter;
        private readonly IMapper _mapper;

        private static List<KeyValuePair<string, string>> IconList = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>(IconNames.Sunny.ToString(), "wi-day-sunny" ),
                new KeyValuePair<string, string>(IconNames.Cloud.ToString(), "wi-day-cloudy" ),
                new KeyValuePair<string, string>(IconNames.Cloudy.ToString(), "wi-cloudy" ),
                new KeyValuePair<string, string>(IconNames.CloudyGusts.ToString(), "wi-cloudy-gusts" ),
                new KeyValuePair<string, string>(IconNames.StrongWind.ToString(), "wi-strong-wind" ),
                new KeyValuePair<string, string>(IconNames.Sprinkle.ToString(), "wi-sprinkle" ),
                new KeyValuePair<string, string>(IconNames.Showers.ToString(), "wi-showers" ),
                new KeyValuePair<string, string>(IconNames.Rain.ToString(), "wi-rain"),
                new KeyValuePair<string, string>(IconNames.RainMix.ToString(), "wi-rain-mix"),
                new KeyValuePair<string, string>(IconNames.Snow.ToString(), "wi-snow"),
                new KeyValuePair<string, string>(IconNames.Hail.ToString(), "wi-hail"),
                new KeyValuePair<string, string>(IconNames.StormShowers.ToString(), "wi-storm-showers"),
                new KeyValuePair<string, string>(IconNames.Lightning.ToString(), "wi-lightning"),
                new KeyValuePair<string, string>(IconNames.Thunderstorm.ToString(), "wi-thunderstorm"),
                new KeyValuePair<string, string>(IconNames.Hot.ToString(), "wi-hot"),
                new KeyValuePair<string, string>(IconNames.Haze.ToString(), "wi-day-haze"),
                new KeyValuePair<string, string>(IconNames.Fog.ToString(), "wi-fog"),
                new KeyValuePair<string, string>(IconNames.SmlCraftAdvis.ToString(), "wi-small-craft-advisory"),
                new KeyValuePair<string, string>(IconNames.GaleWarning.ToString(), "wi-gale-warning"),
                new KeyValuePair<string, string>(IconNames.StormWarning.ToString(), "wi-storm-warning"),
                new KeyValuePair<string, string>(IconNames.HurricaneWarn.ToString(), "wi-hurricane-warning"),
                new KeyValuePair<string, string>(IconNames.Hurricane.ToString(), "wi-hurricane" )
            };

    public WeatherService(
            Core.Services.IClubService clubService,
            Core.Services.IWeatherService weatherService,
            Core.Services.IConversionService converter,
            IMapper mapper)
        {
            _coreClubService = clubService;
            _coreWeatherService = weatherService;
            _converter = converter;
            _mapper = mapper;
        }


        public async Task<WeatherViewModel> ConvertToLocalizedWeather(Weather weather, Guid clubId)
        {
            var club = await _coreClubService.GetMinimalClub(clubId);
            return GetLocalizedWeather(weather, club.WeatherSettings);
        }

        public async Task<WeatherViewModel> GetCurrentWeatherForClubAsync(Guid clubId)
        {
            var club = await _coreClubService.GetMinimalClub(clubId);
            if(club.WeatherSettings?.Latitude == null || club.WeatherSettings?.Longitude == null)
            {
                return null;
            }
            var weather = await _coreWeatherService.GetCurrentWeatherAsync(
                club.WeatherSettings.Latitude.Value,
                club.WeatherSettings.Longitude.Value);
            return GetLocalizedWeather(weather, club.WeatherSettings);
        }

        public IList<KeyValuePair<string, string>> GetWeatherIconOptions()
        {
            return IconList;
        }

        public string GetIconCharacter(string iconName)
        {
            var returnValue = IconList.FirstOrDefault(kvp => kvp.Key == iconName);
            return returnValue.Value;
        }

        public IList<string> GetSpeedUnitOptions()
        {
            return new List<string>
            {
                "MPH",
                "km/h",
                "knots",
                "m/s"
            };
        }

        public IList<string> GetTemperatureUnitOptions()
        {
            return new List<string>
            {
                "Fahrenheit",
                "Celsius"
            };
        }

        private WeatherViewModel GetLocalizedWeather(Weather weather, WeatherSettings settings)
        {
            var weatherVm = new WeatherViewModel
            {
                Description = weather?.Description,
                Icon = weather?.Icon,
                TemperatureUnits = GetTempUnitString(settings?.TemperatureUnits),
                WindSpeedUnits = settings?.WindSpeedUnits,
                Humidity = weather?.Humidity,
                CloudCoverPercent = weather?.CloudCoverPercent
            };

            weatherVm.Temperature = _converter.Convert(
                weather?.TemperatureDegreesKelvin,
                _converter.Kelvin,
                settings?.TemperatureUnits
                )?.ToString("N0")
                ?? weather?.TemperatureString;
            weatherVm.WindSpeed =
                 _converter.Convert(
                     weather?.WindSpeedMeterPerSecond,
                     _converter.MeterPerSecond,
                     settings?.WindSpeedUnits)?.ToString("N0")
                ?? weather?.WindSpeedString;
            weatherVm.WindDirection =
                weather?.WindDirectionDegrees?.ToString()
                ?? weather?.WindDirectionString;
            weatherVm.WindGust =
                 _converter.Convert(
                     weather?.WindGustMeterPerSecond,
                     _converter.MeterPerSecond,
                     settings?.WindSpeedUnits)?.ToString("N0")
                ?? weather?.WindGustString;
            return weatherVm;
        }

        public Weather GetStandardWeather(WeatherViewModel weather)
        {
            var returnObj = new Weather
            {
                Description = weather.Description,
                Icon = weather.Icon,
                TemperatureString = GetTemperatureString(weather),
                TemperatureDegreesKelvin = GetTemperatureDecimal(weather),
                WindSpeedString = GetWindString(weather),
                WindSpeedMeterPerSecond = GetWindMeterPerSecond(weather),
                WindDirectionString = weather.WindDirection,
                WindDirectionDegrees = GetWindDirectionDecimal(weather),
                WindGustString = GetWindGustString(weather),
                WindGustMeterPerSecond = GetGustMeterPerSecond(weather),
                Humidity = weather.Humidity,
                CloudCoverPercent = weather.CloudCoverPercent,
                CreatedDate = DateTime.UtcNow
            };

            return returnObj;
        }

        private string GetTemperatureString(WeatherViewModel weather)
        {
            var tempDecimal = GetTemperatureDecimal(weather);
            if (tempDecimal != null) {
                return tempDecimal?.ToString("N2");
            }

            return weather.Temperature;
        }

        private decimal? GetTemperatureDecimal(WeatherViewModel weather)
        {
            if (String.IsNullOrWhiteSpace(weather.TemperatureUnits))
            {
                return null;
            }
            decimal tempDecimal;
            if(Decimal.TryParse(weather.Temperature, out tempDecimal)){
                var tempKelvin = 
                    _converter.Convert(
                        tempDecimal,
                        weather.TemperatureUnits,
                        _converter.Kelvin);
                return tempKelvin;
            } else
            {
                return null;
            }
        }

        private string GetWindString(WeatherViewModel weather)
        {
            var windDecimal = GetWindMeterPerSecond(weather);
            if (windDecimal != null)
            {
                return windDecimal?.ToString("N2");
            }

            return weather.WindSpeed;
        }
        private decimal? GetWindMeterPerSecond(WeatherViewModel weather)
        {
            if (String.IsNullOrWhiteSpace(weather.WindSpeedUnits))
            {
                return null;
            }
            decimal windDecimal;
            if (Decimal.TryParse(weather.WindSpeed, out windDecimal))
            {
                return _converter.Convert(windDecimal, weather.WindSpeedUnits,
                    _converter.MeterPerSecond);
            }
            else
            {
                return null;
            }
        }

        private decimal? GetWindDirectionDecimal(WeatherViewModel weather)
        {
            decimal directionDecimal;
            if (Decimal.TryParse(weather.WindDirection, out directionDecimal))
            {
                return directionDecimal;
            }
            else
            {
                return GetWindDirectionFromLetters(weather.WindDirection);
            }
        }

        private decimal? GetWindDirectionFromLetters(string windDirection)
        {
            if(String.IsNullOrWhiteSpace(windDirection))
            {
                return null;
            }
            var allCaps = windDirection.ToUpperInvariant();
            switch (allCaps)
            {
                case "N":
                    return 0;
                case "E":
                    return 90;
                case "S":
                    return 180;
                case "W":
                    return 270;
                case "NE":
                    return 45;
                case "SE":
                    return 135;
                case "SW":
                    return 225;
                case "NW":
                    return 315;
                case "NNE":
                    return 23;
                case "ENE":
                    return 68;
                case "ESE":
                    return 113;
                case "SSE":
                    return 158;
                case "SSW":
                    return 203;
                case "WSW":
                    return 248;
                case "WNW":
                    return 293;
                case "NNW":
                    return 338;
                default:
                    return null;
            }
        }

        private string GetWindGustString(WeatherViewModel weather)
        {
            var windDecimal = GetGustMeterPerSecond(weather);
            if (windDecimal != null)
            {
                return windDecimal?.ToString("N2");
            }

            return weather.WindGust;
        }
        private decimal? GetGustMeterPerSecond(WeatherViewModel weather)
        {
            if (String.IsNullOrWhiteSpace(weather.WindSpeedUnits))
            {
                return null;
            }
            decimal windDecimal;
            if (Decimal.TryParse(weather.WindGust, out windDecimal))
            {
                return _converter.Convert(windDecimal, weather.WindSpeedUnits,
                    _converter.MeterPerSecond);
            }
            else
            {
                return null;
            }
        }

        private string GetTempUnitString(string temperatureUnits)
        {
            if (string.IsNullOrWhiteSpace(temperatureUnits))
            {
                return null;
            }
            if(temperatureUnits.ToUpperInvariant().StartsWith("F")
                || temperatureUnits.ToUpperInvariant().Contains("FAHR"))
            {
                return "°F";
            }
            return "°C";
        }

    }
}

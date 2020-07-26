using AutoMapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using SailScores.Core.Model.OpenWeatherMap;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace SailScores.Core.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IConfiguration _configuration;
        private readonly IMapper _mapper;

        private readonly string _openWeatherApiRoot = "https://api.openweathermap.org/data/2.5/weather";

        public WeatherService(
            IHttpClientFactory clientFactory,
            IConfiguration configuration,
            IMapper mapper)
        {
            _clientFactory = clientFactory;
            _configuration = configuration;
            _mapper = mapper;
        }

        public async Task<Weather> GetCurrentWeatherAsync(
            decimal latitude,
            decimal longitude)
        {

            var builder = new UriBuilder(_openWeatherApiRoot);
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["lat"] = latitude.ToString(CultureInfo.InvariantCulture);
            query["lon"] = longitude.ToString(CultureInfo.InvariantCulture);
            query["APPID"] = _configuration["OpenWeatherMapAppId"];
            builder.Query = query.ToString();
            string url = builder.ToString();


            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                url);
            using var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content
                    .ReadAsStringAsync().ConfigureAwait(false);
                var responseObj = JsonConvert.DeserializeObject<CurrentWeatherResponse>(responseString);
                var domainObj = _mapper.Map<Weather>(responseObj);
                domainObj.Icon = GetIconName(responseObj.Weather?.First()?.Id);
                return domainObj;
            }
            else
            {
                return null;
            }
        }

        private string GetIconName(int? id)
        {
            if (!id.HasValue)
            {
                return null;
            }

            switch (id.Value)
            {
                case 800:
                    return IconNames.Sunny.ToString();
                case 200:
                case 201:
                case 202:
                    return IconNames.Thunderstorm.ToString();
                case 210:
                case 211:
                case 212:
                case 221:
                    return IconNames.Lightning.ToString();
                case 230:
                case 231:
                case 232:
                    return IconNames.Thunderstorm.ToString();
                case 300:
                case 301:
                case 321:
                case 500:
                    return IconNames.Sprinkle.ToString();
                case 302:
                case 311:
                case 312:
                case 314:
                case 501:
                case 502:
                case 503:
                case 504:
                    return IconNames.Rain.ToString();
                case 310:
                case 511:
                case 611:// was sleet
                case 612:
                case 615:
                case 616:
                case 620:
                    return IconNames.RainMix.ToString();
                case 313:
                case 520:
                case 521:
                case 522:
                case 701:
                    return IconNames.Showers.ToString();
                case 531:
                case 901:
                    return IconNames.StormShowers.ToString();
                case 600:
                case 601:
                case 602:
                case 621:
                case 622:
                    return IconNames.Snow.ToString();

                case 711: //was smoke
                case 721:
                case 731: // was dust
                case 761: // was dust
                case 762:  // was dust
                    return IconNames.Haze.ToString();
                case 741:
                    return IconNames.Fog.ToString();
                case 771:
                case 801:
                case 802:
                    return IconNames.Cloud.ToString();
                case 803:
                case 804:
                    return IconNames.Cloudy.ToString();
                case 781: // was tornado
                case 900:// was tornado
                case 905:
                case 957:
                    return IconNames.StrongWind.ToString();
                case 902:
                    return IconNames.Hurricane.ToString();
                case 903: // was snowflake - cold
                    return IconNames.Snow.ToString();
                case 904:
                    return IconNames.Hot.ToString();
                case 906:
                    return IconNames.Hail.ToString();
            }

            return null;
        }
    }
}

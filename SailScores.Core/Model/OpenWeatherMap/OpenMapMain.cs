using Newtonsoft.Json;

namespace SailScores.Core.Model.OpenWeatherMap
{
    public class OpenMapMain
    {
        [JsonProperty("temp")]
        public decimal TemperatureDegreesKelvin { get; set; }

        [JsonProperty("feels_like")]
        public decimal? FeelsLikeDegreesKelvin { get; set; }

        [JsonProperty("humidity")]
        public decimal? HumidityPercent { get; set; }


    }
}
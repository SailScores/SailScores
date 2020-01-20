using Newtonsoft.Json;

namespace SailScores.Core.Model.OpenWeatherMap
{
    public class Coordinates
    {
        [JsonProperty("lon")]
        public decimal Longitude { get; set; }
        [JsonProperty("lat")]
        public decimal Latitude { get; set; }
    }
}